using AtelieBebe.Backoffice.Core.Domain.Entities;

namespace AtelieBebe.Backoffice.Core.Domain.Repositories;

public interface IAuditLogRepository
{
    void Add(AuditLog log);
    Task<(IReadOnlyList<AuditLog> Items, int TotalItems)> ListAsync(int page, int pageSize, CancellationToken ct = default);
}
