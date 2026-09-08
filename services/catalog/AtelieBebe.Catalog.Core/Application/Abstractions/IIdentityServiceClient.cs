namespace AtelieBebe.Catalog.Core.Application.Abstractions;

public sealed record IdentityCustomerInfo(Guid Id, string Name, string Email, bool IsAnonymized);

/// <summary>Used only by the wishlist reminder job — needs the customer's current name/e-mail to build the reminder.</summary>
public interface IIdentityServiceClient
{
    Task<IdentityCustomerInfo?> GetCustomerAsync(Guid customerId, CancellationToken ct = default);
}
