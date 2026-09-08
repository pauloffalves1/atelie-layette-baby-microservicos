using System.Text.Json;
using AtelieBebe.Notifications.Worker.Abstractions;
using AtelieBebe.Notifications.Worker.ExternalServices;
using AtelieBebe.SharedKernel.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Notifications.Worker.Consumers;

/// <summary>
/// Subscribes to every domain event that used to be dispatched in-process by the monolith's
/// OutboxProcessor.DispatchAsync switch, and does the same two-channel (WhatsApp + e-mail) fan-out —
/// just fed by RabbitMQ instead of a shared database. Each event's shape below is this consumer's
/// own contract (matched by property name via JSON, not a shared CLR type) since the producing
/// service's actual domain-event class lives in a different assembly/process.
/// </summary>
public sealed class NotificationsEventConsumer : RabbitMqEventConsumerBase
{
    public NotificationsEventConsumer(IOptions<RabbitMqOptions> options, IServiceScopeFactory scopeFactory, ILogger<NotificationsEventConsumer> logger)
        : base(options, scopeFactory, logger, queueName: "notifications.events",
            routingKeys:
            [
                "OrderCreatedDomainEvent",
                "OrderStatusChangedDomainEvent",
                "CustomerRegisteredDomainEvent",
                "ContactMessageReceivedDomainEvent",
                "PasswordResetRequestedDomainEvent",
                "EmailVerificationRequestedDomainEvent",
                "ProductBackInStockDomainEvent",
                "AbandonedCartReminderDomainEvent",
                "WishlistReminderDomainEvent",
            ])
    {
    }

    protected override async Task HandleAsync(string eventType, string jsonContent, IServiceProvider scopedServices, CancellationToken ct)
    {
        var sender = scopedServices.GetRequiredService<INotificationSender>();
        var emailSender = scopedServices.GetRequiredService<IEmailSender>();
        var catalogClient = scopedServices.GetRequiredService<ICatalogServiceClient>();
        var identityClient = scopedServices.GetRequiredService<IIdentityServiceClient>();
        var appUrls = scopedServices.GetRequiredService<IAppUrlProvider>();
        var logger = scopedServices.GetRequiredService<ILogger<NotificationsEventConsumer>>();

        switch (eventType)
        {
            case "OrderCreatedDomainEvent":
            {
                var e = Deserialize<OrderCreatedEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendOrderCreatedAsync(e.OrderId, e.CustomerName, e.CustomerEmail, e.TotalAmount, ct), logger);
                await TrySendEmailAsync(() => emailSender.SendNewOrderAdminAlertAsync(e.OrderId, e.CustomerName, e.TotalAmount, ct), logger);
                await sender.SendOrderCreatedAsync(e.OrderId, e.CustomerName, e.CustomerPhone, e.TotalAmount, ct);
                await sender.SendNewOrderAdminAlertAsync(e.OrderId, e.CustomerName, e.TotalAmount, ct);
                break;
            }
            case "PasswordResetRequestedDomainEvent":
            {
                var e = Deserialize<PasswordResetRequestedEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendPasswordResetAsync(e.Name, e.Email, e.ResetUrl, ct), logger);
                break;
            }
            case "EmailVerificationRequestedDomainEvent":
            {
                var e = Deserialize<EmailVerificationRequestedEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendEmailVerificationAsync(e.Name, e.Email, e.VerificationUrl, ct), logger);
                break;
            }
            case "OrderStatusChangedDomainEvent":
            {
                var e = Deserialize<OrderStatusChangedEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendOrderStatusChangedAsync(e.OrderId, e.CustomerName, e.CustomerEmail, e.OldStatus, e.NewStatus, ct), logger);
                await sender.SendOrderStatusChangedAsync(e.OrderId, e.CustomerName, e.CustomerPhone, e.OldStatus, e.NewStatus, ct);
                break;
            }
            case "CustomerRegisteredDomainEvent":
            {
                var e = Deserialize<CustomerRegisteredEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendWelcomeMessageAsync(e.CustomerId, e.Name, e.Email, ct), logger);
                await sender.SendWelcomeMessageAsync(e.CustomerId, e.Name, e.Phone, ct);
                break;
            }
            case "ContactMessageReceivedDomainEvent":
            {
                var e = Deserialize<ContactMessageReceivedEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendContactAcknowledgementAsync(e.MessageId, e.Name, e.Email, ct), logger);
                await sender.SendContactAcknowledgementAsync(e.MessageId, e.Name, e.Phone, ct);
                break;
            }
            case "ProductBackInStockDomainEvent":
            {
                // The event itself only carries product data (Catalog's own concern); who to
                // notify is resolved here, the same two service-to-service lookups the monolith
                // did in-process at outbox-dispatch time (wishlist items, then each customer).
                var e = Deserialize<ProductBackInStockEvent>(jsonContent);
                var productUrl = $"{appUrls.PublicUrl.TrimEnd('/')}/produto/{e.ProductSlug}";
                var customerIds = await catalogClient.GetWishlistingCustomerIdsAsync(e.ProductId, ct);
                foreach (var customerId in customerIds)
                {
                    var customer = await identityClient.GetCustomerAsync(customerId, ct);
                    if (customer is null || customer.IsAnonymized) continue;
                    await TrySendEmailAsync(() => emailSender.SendProductBackInStockAsync(customer.Name, customer.Email, e.ProductName, productUrl, ct), logger);
                }
                break;
            }
            case "AbandonedCartReminderDomainEvent":
            {
                var e = Deserialize<AbandonedCartReminderEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendAbandonedCartReminderAsync(e.CustomerName, e.CustomerEmail,
                    e.Items.Select(i => new AbandonedCartItem(i.ProductName, i.ProductUrl, i.Quantity)).ToList(), e.ShopUrl, ct), logger);
                break;
            }
            case "WishlistReminderDomainEvent":
            {
                var e = Deserialize<WishlistReminderEvent>(jsonContent);
                await TrySendEmailAsync(() => emailSender.SendWishlistReminderAsync(e.CustomerName, e.CustomerEmail, e.ProductName, e.ProductUrl, ct), logger);
                break;
            }
            default:
                logger.LogWarning("Nenhum handler registrado para o evento {EventType}.", eventType);
                break;
        }
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException($"Não foi possível desserializar o evento {typeof(T).Name}.");

    private static async Task TrySendEmailAsync(Func<Task> send, ILogger logger)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar e-mail de notificação (canal independente do WhatsApp, não bloqueia o restante).");
        }
    }

    private sealed record OrderCreatedEvent(Guid OrderId, string CustomerName, string CustomerEmail, string CustomerPhone, decimal TotalAmount);
    private sealed record OrderStatusChangedEvent(Guid OrderId, string CustomerName, string CustomerEmail, string CustomerPhone, string OldStatus, string NewStatus);
    private sealed record CustomerRegisteredEvent(Guid CustomerId, string Name, string Email, string Phone);
    private sealed record ContactMessageReceivedEvent(Guid MessageId, string Name, string Email, string Phone);
    private sealed record PasswordResetRequestedEvent(string Name, string Email, string ResetUrl);
    private sealed record EmailVerificationRequestedEvent(string Name, string Email, string VerificationUrl);
    private sealed record ProductBackInStockEvent(Guid ProductId, string ProductName, string ProductSlug);
    private sealed record AbandonedCartReminderItem(string ProductName, string ProductUrl, int Quantity);
    private sealed record AbandonedCartReminderEvent(string CustomerName, string CustomerEmail, IReadOnlyList<AbandonedCartReminderItem> Items, string ShopUrl);
    private sealed record WishlistReminderEvent(string CustomerName, string CustomerEmail, string ProductName, string ProductUrl);
}
