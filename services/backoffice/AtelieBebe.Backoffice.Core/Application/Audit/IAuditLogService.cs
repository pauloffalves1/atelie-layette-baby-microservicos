using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Application.Audit;

public interface IAuditLogService
{
    Task RecordAsync(Guid adminId, string adminName, string action, string details, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> ListAsync(int page, int pageSize, AuditLogFilter? filter = null, CancellationToken ct = default);
    Task<AuditLogFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default);
}
