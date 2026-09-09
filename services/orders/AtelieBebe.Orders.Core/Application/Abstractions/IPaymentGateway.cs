namespace AtelieBebe.Orders.Core.Application.Abstractions;

public sealed record CardChargeResult(bool Approved, string Status, string? ExternalId, string? DeclineReason);

public sealed record PixCharge(string ExternalId, string QrCodeText, string? QrCodeImageUrl);

public sealed record PaymentDetails(string Status, string? ExternalReference);

/// <summary>
/// Payment provider boundary (currently PagBank), using its direct Order-creation API — the
/// customer's card is encrypted in their own browser (never touches our server) and submitted
/// together with the order, so the charge result (approved/declined) is known synchronously; PIX
/// works the same way but returns a QR code to display on our own checkout instead of an
/// immediate charge result. Never called from a use case as a hard dependency for the checkout to
/// succeed — both charge methods return null when the gateway isn't configured yet (blank access
/// token), matching how INotificationSender degrades gracefully when unconfigured.
/// </summary>
public interface IPaymentGateway
{
    bool IsConfigured { get; }

    /// <summary>RSA public key the frontend's PagBank SDK uses to encrypt card data client-side before it ever reaches us.</summary>
    Task<string?> GetCardEncryptionPublicKeyAsync(CancellationToken ct = default);

    Task<CardChargeResult?> ChargeCardAsync(
        Guid orderId, string description, decimal amount,
        string customerName, string customerEmail, string customerTaxId, string? customerPhone,
        string encryptedCard, int installments, CancellationToken ct = default);

    Task<PixCharge?> CreatePixChargeAsync(
        Guid orderId, string description, decimal amount,
        string customerName, string customerEmail, string customerTaxId, string? customerPhone,
        CancellationToken ct = default);

    /// <summary>Re-queries PagBank's Order resource — used to reconcile a PIX charge once the webhook (or a manual status check) reports it paid.</summary>
    Task<PaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken ct = default);
}
