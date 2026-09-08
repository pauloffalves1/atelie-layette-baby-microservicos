using AtelieBebe.Orders.Core.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Orders.Core.Infrastructure.Payments;

/// <summary>
/// Stands in for PagBankGateway in local development, before the ateliê has real PagBank
/// credentials — lets the checkout → payment → confirmation flow be previewed end-to-end
/// without calling any real API. "Payment" here is just a redirect to our own SPA page
/// (<see cref="AppUrlOptions.PublicUrl"/>/pagamento-simulado/{orderId}), which posts straight to
/// OrderService.SimulatePaymentAsync — GetPaymentAsync is never actually called in this flow.
/// Only ever wired up in Development (see DependencyInjection.AddInfrastructure); production
/// with a blank token must fall back to PagBankGateway (IsConfigured = false, no payment step
/// at all), never to a fake approval.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly AppUrlOptions _appUrls;

    public bool IsConfigured => true;

    public FakePaymentGateway(IOptions<AppUrlOptions> appUrls) => _appUrls = appUrls.Value;

    public Task<PaymentPreference?> CreatePreferenceAsync(Guid orderId, string description, decimal amount, string customerEmail, CancellationToken ct = default) =>
        Task.FromResult<PaymentPreference?>(new PaymentPreference($"{_appUrls.PublicUrl}/pagamento-simulado/{orderId}", $"FAKE-{orderId}"));

    public Task<PaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken ct = default) =>
        Task.FromResult<PaymentDetails?>(null);
}
