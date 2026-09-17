using AtelieBebe.Backoffice.Core.Domain.Entities;

namespace AtelieBebe.Backoffice.Core.Domain.Repositories;

public interface IAuditLogRepository
{
    void Add(AuditLog log);
    Task<(IReadOnlyList<AuditLog> Items, int TotalItems)> ListAsync(int page, int pageSize, AtelieBebe.Backoffice.Core.Application.Audit.AuditLogFilter filter, CancellationToken ct = default);
    Task<(IReadOnlyList<string> Admins, IReadOnlyList<string> Actions)> ListFilterOptionsAsync(CancellationToken ct = default);
}
