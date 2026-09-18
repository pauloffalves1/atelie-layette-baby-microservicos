namespace AtelieBebe.Identity.Core.Application.Customers;

public interface ICustomerAdminService
{
    Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<CustomerSummaryDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerSummaryDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerSummaryDto> VerifyEmailAsync(Guid id, CancellationToken ct = default);

    /// <summary>Approves/revokes a customer as a test user (RF40). Takes effect on her next login — the flag rides in the token.</summary>
    Task<CustomerSummaryDto> SetTestAsync(Guid id, bool isTest, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
