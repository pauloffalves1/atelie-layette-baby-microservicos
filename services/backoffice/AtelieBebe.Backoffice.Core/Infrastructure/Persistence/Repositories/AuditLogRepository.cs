using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.Backoffice.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly BackofficeDbContext _dbContext;

    public AuditLogRepository(BackofficeDbContext dbContext) => _dbContext = dbContext;

    public void Add(AuditLog log) => _dbContext.AuditLogs.Add(log);

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalItems)> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.AuditLogs.OrderByDescending(a => a.CreatedAt);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var totalItems = await _dbContext.AuditLogs.CountAsync(ct);

        return (items, totalItems);
    }
}
