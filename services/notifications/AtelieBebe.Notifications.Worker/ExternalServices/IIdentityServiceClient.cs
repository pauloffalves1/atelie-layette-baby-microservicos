namespace AtelieBebe.Notifications.Worker.ExternalServices;

public sealed record IdentityCustomerInfo(Guid Id, string Name, string Email, bool IsAnonymized);

public interface IIdentityServiceClient
{
    Task<IdentityCustomerInfo?> GetCustomerAsync(Guid customerId, CancellationToken ct = default);
}
