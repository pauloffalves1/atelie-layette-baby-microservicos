using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.SharedKernel.Outbox;

/// <summary>Implemented by every microservice's DbContext so the shared outbox publisher can poll it generically.</summary>
public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
