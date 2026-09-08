namespace AtelieBebe.Notifications.Worker.Abstractions;

/// <summary>
/// Side-effect boundary invoked by the outbox processor — never called directly from a use case,
/// so a slow or failing notification channel can never block or fail the request that created the event.
/// </summary>
public interface INotificationSender
{
    Task SendOrderCreatedAsync(Guid orderId, string customerName, string customerPhone, decimal total, CancellationToken ct = default);
    Task SendOrderStatusChangedAsync(Guid orderId, string customerName, string customerPhone, string oldStatus, string newStatus, CancellationToken ct = default);
    Task SendWelcomeMessageAsync(Guid customerId, string name, string phone, CancellationToken ct = default);
    Task SendContactAcknowledgementAsync(Guid messageId, string name, string phone, CancellationToken ct = default);

    /// <summary>Alerts the ateliê's own WhatsApp (Admin:NotificationPhone) that a new order came in — a second, admin-facing message for the same OrderCreated event, not a reply to the customer.</summary>
    Task SendNewOrderAdminAlertAsync(Guid orderId, string customerName, decimal total, CancellationToken ct = default);
}
