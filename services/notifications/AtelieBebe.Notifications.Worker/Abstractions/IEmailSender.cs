namespace AtelieBebe.Notifications.Worker.Abstractions;

public sealed record AbandonedCartItem(string ProductName, string ProductUrl, int Quantity);

/// <summary>
/// Independent notification channel alongside INotificationSender (WhatsApp) — dispatched
/// separately by the outbox processor so a failure/misconfiguration in one channel never blocks
/// the other. Implementations should throw when not configured, same as WhatsAppNotificationSender;
/// the caller is responsible for catching and logging rather than letting it fail the whole batch.
/// </summary>
public interface IEmailSender
{
    Task SendOrderCreatedAsync(Guid orderId, string customerName, string customerEmail, decimal total, CancellationToken ct = default);
    Task SendOrderStatusChangedAsync(Guid orderId, string customerName, string customerEmail, string oldStatus, string newStatus, CancellationToken ct = default);
    Task SendWelcomeMessageAsync(Guid customerId, string name, string email, CancellationToken ct = default);
    Task SendContactAcknowledgementAsync(Guid messageId, string name, string email, CancellationToken ct = default);

    /// <summary>Alerts the ateliê's own inbox (Admin:NotificationEmail) that a new order came in — a second, admin-facing e-mail for the same OrderCreated event, not a reply to the customer.</summary>
    Task SendNewOrderAdminAlertAsync(Guid orderId, string customerName, decimal total, CancellationToken ct = default);

    /// <summary>Only channel used for password resets — WhatsApp templates need Meta pre-approval, which a reset link's one-off nature doesn't justify.</summary>
    Task SendPasswordResetAsync(string name, string email, string resetUrl, CancellationToken ct = default);

    /// <summary>Same reasoning as password reset — e-mail only, no WhatsApp template for a one-off confirmation link.</summary>
    Task SendEmailVerificationAsync(string name, string email, string verificationUrl, CancellationToken ct = default);

    /// <summary>Sent to every customer with the product on their wishlist when it goes from inactive back to active.</summary>
    Task SendProductBackInStockAsync(string customerName, string customerEmail, string productName, string productUrl, CancellationToken ct = default);

    /// <summary>Sent once per abandoned cart — links to each product directly rather than assuming the customer's local cart is still intact.</summary>
    Task SendAbandonedCartReminderAsync(string customerName, string customerEmail, IReadOnlyList<AbandonedCartItem> items, string shopUrl, CancellationToken ct = default);

    /// <summary>Sent once, a few days after a customer adds a product to their wishlist and hasn't purchased it yet.</summary>
    Task SendWishlistReminderAsync(string customerName, string customerEmail, string productName, string productUrl, CancellationToken ct = default);
}
