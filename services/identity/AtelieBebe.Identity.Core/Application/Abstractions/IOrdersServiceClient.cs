namespace AtelieBebe.Identity.Core.Application.Abstractions;

/// <summary>
/// The one real cross-service dependency Identity has: deleting a customer account needs to know
/// whether Orders still holds purchase history for them (if so, the account is anonymized instead
/// of removed — order history always survives). Calls Orders' internal API, never exposed to the
/// browser through the Gateway.
/// </summary>
public interface IOrdersServiceClient
{
    Task<bool> CustomerHasOrdersAsync(Guid customerId, CancellationToken ct = default);
}
