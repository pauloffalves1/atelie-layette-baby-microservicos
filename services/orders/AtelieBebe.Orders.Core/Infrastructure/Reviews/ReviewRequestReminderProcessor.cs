using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Orders.Core.Infrastructure.Reviews;

/// <summary>
/// Polls for orders delivered a few days ago that haven't had a review-request e-mail sent yet, and
/// raises one ReviewRequestDomainEvent per order (Notifications sends the actual e-mail — see
/// ReviewRequestDomainEvent). Same time-based-job shape as AbandonedCartReminderProcessor: independent
/// of the outbox's normal SaveChanges-triggered dispatch, but still goes through the same outbox table.
/// </summary>
public sealed class ReviewRequestReminderProcessor : BackgroundService
{
    private static readonly TimeSpan WaitAfterDelivery = TimeSpan.FromDays(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReviewRequestReminderProcessor> _logger;

    public ReviewRequestReminderProcessor(IServiceScopeFactory scopeFactory, ILogger<ReviewRequestReminderProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar lembretes de avaliação pós-entrega.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrdersUnitOfWork>();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ICatalogServiceClient>();
        var appUrls = scope.ServiceProvider.GetRequiredService<IAppUrlProvider>();

        var cutoff = DateTime.UtcNow - WaitAfterDelivery;
        var delivered = await unitOfWork.Orders.ListDeliveredWithoutReviewReminderAsync(cutoff, ct);
        if (delivered.Count == 0) return;

        var siteUrl = appUrls.PublicUrl.TrimEnd('/');

        foreach (var order in delivered)
        {
            var items = new List<ReviewRequestItem>();
            foreach (var orderItem in order.Items)
            {
                if (orderItem.ProductId is not { } productId) continue;

                var product = await catalogClient.GetProductAsync(productId, ct);
                if (product is null || !product.Active) continue;

                items.Add(new ReviewRequestItem(product.Name, $"{siteUrl}/produto/{product.Slug}"));
            }

            order.MarkReviewReminderSent(items);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
