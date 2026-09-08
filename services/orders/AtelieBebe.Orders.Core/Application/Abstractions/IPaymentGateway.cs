namespace AtelieBebe.Orders.Core.Application.Abstractions;

public sealed record PaymentPreference(string CheckoutUrl, string? ExternalId);

public sealed record PaymentDetails(string Status, string? ExternalReference);

/// <summary>
/// Payment provider boundary (currently PagBank). Never called from a use case as a hard
/// dependency for the checkout to succeed — CreatePreferenceAsync returns null when the gateway
/// isn't configured yet (blank access token), and the order is still created normally without a
/// payment redirect, matching how INotificationSender degrades gracefully when unconfigured.
/// </summary>
public interface IPaymentGateway
{
    bool IsConfigured { get; }

    Task<PaymentPreference?> CreatePreferenceAsync(Guid orderId, string description, decimal amount, string customerEmail, CancellationToken ct = default);

    Task<PaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken ct = default);
}
