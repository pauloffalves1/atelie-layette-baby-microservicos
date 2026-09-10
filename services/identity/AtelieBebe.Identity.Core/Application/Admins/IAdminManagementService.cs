namespace AtelieBebe.Identity.Core.Application.Admins;

public interface IAdminManagementService
{
    Task<IReadOnlyList<AdminSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<AdminSummaryDto> CreateAsync(CreateAdminRequest request, CancellationToken ct = default);
    Task<AdminSummaryDto> UpdatePermissionsAsync(Guid adminId, UpdateAdminPermissionsRequest request, CancellationToken ct = default);
    Task<AdminSummaryDto> RemoveAsync(Guid adminId, Guid requestedByAdminId, CancellationToken ct = default);
}
