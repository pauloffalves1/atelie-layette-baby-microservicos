using AtelieBebe.Catalog.Core.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Infrastructure.Wishlist;

/// <summary>
/// Polls for wishlist items that have sat unpurchased for a while, and raises a reminder domain
/// event once per stale item (Notifications sends the actual e-mail — see
/// WishlistReminderDomainEvent). Independent of the outbox's normal trigger (a SaveChanges after a
/// business action) — this is a time-based check, so it runs as its own periodic job, but still
/// goes through the same outbox table for the event it raises.
/// </summary>
public sealed class WishlistReminderProcessor : BackgroundService
{
    private static readonly TimeSpan ReminderAfter = TimeSpan.FromDays(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WishlistReminderProcessor> _logger;

    public WishlistReminderProcessor(IServiceScopeFactory scopeFactory, ILogger<WishlistReminderProcessor> logger)
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
                _logger.LogError(ex, "Falha ao processar lembretes de lista de desejos.");
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
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ICatalogUnitOfWork>();
        var identityClient = scope.ServiceProvider.GetRequiredService<IIdentityServiceClient>();
        var appUrls = scope.ServiceProvider.GetRequiredService<IAppUrlProvider>();

        var cutoff = DateTime.UtcNow - ReminderAfter;
        var stale = await unitOfWork.WishlistItems.ListStaleAsync(cutoff, ct);
        if (stale.Count == 0) return;

        var siteUrl = appUrls.PublicUrl.TrimEnd('/');

        foreach (var item in stale)
        {
            var product = await unitOfWork.Products.GetByIdAsync(item.ProductId, ct);
            if (product is null || !product.Active) continue;

            var customer = await identityClient.GetCustomerAsync(item.CustomerId, ct);
            if (customer is null || customer.IsAnonymized) continue;

            item.MarkReminderSent(customer.Name, customer.Email, product.Name, $"{siteUrl}/produto/{product.Slug}");
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
