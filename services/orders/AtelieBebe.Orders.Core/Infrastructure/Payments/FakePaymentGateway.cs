using AtelieBebe.Orders.Core.Application.Abstractions;

namespace AtelieBebe.Orders.Core.Infrastructure.Payments;

/// <summary>
/// Stands in for PagBankGateway in local development, before the ateliê has real PagBank
/// credentials — cards are always auto-approved instantly (no real encryption/charge happens) and
/// PIX returns a fixed fake code, so the checkout → confirmation flow can be previewed end-to-end
/// without calling any real API. Only ever wired up in Development (see
/// DependencyInjection.AddInfrastructure); production with a blank token must fall back to
/// PagBankGateway (IsConfigured = false, no payment step at all), never to a fake approval.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public bool IsConfigured => true;

    public Task<string?> GetCardEncryptionPublicKeyAsync(CancellationToken ct = default) =>
        Task.FromResult<string?>("fake-public-key-for-local-development");

    public Task<CardChargeResult?> ChargeCardAsync(
        Guid orderId, string description, decimal amount,
        string customerName, string customerEmail, string customerTaxId, string? customerPhone,
        string encryptedCard, int installments, CancellationToken ct = default) =>
        Task.FromResult<CardChargeResult?>(new CardChargeResult(true, "approved", $"FAKE-{orderId}", null));

    public Task<PixCharge?> CreatePixChargeAsync(
        Guid orderId, string description, decimal amount,
        string customerName, string customerEmail, string customerTaxId, string? customerPhone,
        CancellationToken ct = default) =>
        Task.FromResult<PixCharge?>(new PixCharge($"FAKE-{orderId}", "00020126FAKE-PIX-CODE-FOR-LOCAL-DEV-ONLY5204000053039865802BR", null));

    public Task<PaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken ct = default) =>
        Task.FromResult<PaymentDetails?>(null);
}
