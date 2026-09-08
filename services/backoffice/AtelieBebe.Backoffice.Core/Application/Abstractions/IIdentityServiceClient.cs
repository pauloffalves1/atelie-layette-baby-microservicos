namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Backoffice's read-only dependency on Identity — customer count for the dashboard.</summary>
public interface IIdentityServiceClient
{
    Task<int> GetCustomerCountAsync(CancellationToken ct = default);
}
