using System.Text.Json;
using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Cart;
using AtelieBebe.Orders.Core.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Orders.Core.Infrastructure.Cart;

/// <summary>
/// Polls for cart snapshots that went quiet for a while and never turned into an order, and raises
/// a reminder domain event once per abandoned cart (Notifications sends the actual e-mail — see
/// AbandonedCartReminderDomainEvent). Independent of the outbox's normal trigger (a SaveChanges
/// after a business action) — this is a time-based check, so it runs as its own periodic job, but
/// still goes through the same outbox table for the event it raises.
/// </summary>
public sealed class AbandonedCartReminderProcessor : BackgroundService
{
    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedCartReminderProcessor> _logger;

    public AbandonedCartReminderProcessor(IServiceScopeFactory scopeFactory, ILogger<AbandonedCartReminderProcessor> logger)
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
                _logger.LogError(ex, "Falha ao processar lembretes de carrinho abandonado.");
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
        var identityClient = scope.ServiceProvider.GetRequiredService<IIdentityServiceClient>();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ICatalogServiceClient>();
        var appUrls = scope.ServiceProvider.GetRequiredService<IAppUrlProvider>();

        var cutoff = DateTime.UtcNow - AbandonedAfter;
        var abandoned = await unitOfWork.CartSnapshots.ListAbandonedAsync(cutoff, ct);
        if (abandoned.Count == 0) return;

        var siteUrl = appUrls.PublicUrl.TrimEnd('/');

        foreach (var snapshot in abandoned)
        {
            var customer = await identityClient.GetCustomerAsync(snapshot.CustomerId, ct);
            if (customer is null || customer.IsAnonymized)
            {
                snapshot.MarkReminderSent(string.Empty, string.Empty, [], siteUrl);
                continue;
            }

            var savedItems = JsonSerializer.Deserialize<List<CartSyncItemDto>>(snapshot.ItemsJson) ?? [];
            var reminderItems = new List<AbandonedCartReminderItem>();
            foreach (var item in savedItems)
            {
                var product = await catalogClient.GetProductAsync(item.ProductId, ct);
                if (product is null) continue;
                reminderItems.Add(new AbandonedCartReminderItem(product.Name, $"{siteUrl}/produto/{product.Slug}", item.Quantity));
            }

            snapshot.MarkReminderSent(customer.Name, customer.Email, reminderItems, $"{siteUrl}/loja");
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
