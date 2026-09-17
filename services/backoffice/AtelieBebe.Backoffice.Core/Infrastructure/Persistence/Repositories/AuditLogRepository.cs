using AtelieBebe.Backoffice.Core.Application.Audit;
using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.Backoffice.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly BackofficeDbContext _dbContext;

    public AuditLogRepository(BackofficeDbContext dbContext) => _dbContext = dbContext;

    public void Add(AuditLog log) => _dbContext.AuditLogs.Add(log);

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalItems)> ListAsync(int page, int pageSize, AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = _dbContext.AuditLogs.AsQueryable();
        if (filter.AdminName is not null) query = query.Where(a => a.AdminName == filter.AdminName);
        if (filter.Action is not null) query = query.Where(a => a.Action == filter.Action);
        if (filter.FromUtc is { } from) query = query.Where(a => a.CreatedAt >= from);
        if (filter.ToUtcExclusive is { } to) query = query.Where(a => a.CreatedAt < to);
        if (filter.Search is not null) query = query.Where(a => a.Details.Contains(filter.Search));

        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        // Counted over the same filters — it used to count the whole table, so pagination was wrong
        // as soon as anything narrowed the list.
        var totalItems = await query.CountAsync(ct);

        return (items, totalItems);
    }

    public async Task<(IReadOnlyList<string> Admins, IReadOnlyList<string> Actions)> ListFilterOptionsAsync(CancellationToken ct = default)
    {
        var admins = await _dbContext.AuditLogs.Select(a => a.AdminName).Distinct().OrderBy(n => n).ToListAsync(ct);
        var actions = await _dbContext.AuditLogs.Select(a => a.Action).Distinct().OrderBy(n => n).ToListAsync(ct);
        return (admins, actions);
    }
}
