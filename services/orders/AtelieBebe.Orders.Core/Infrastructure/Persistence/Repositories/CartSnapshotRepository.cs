using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence.Repositories;

public sealed class CartSnapshotRepository : ICartSnapshotRepository
{
    private readonly OrdersDbContext _dbContext;

    public CartSnapshotRepository(OrdersDbContext dbContext) => _dbContext = dbContext;

    public Task<CartSnapshot?> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        _dbContext.CartSnapshots.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

    public async Task<IReadOnlyList<CartSnapshot>> ListAbandonedAsync(DateTime updatedBeforeUtc, CancellationToken ct = default) =>
        await _dbContext.CartSnapshots
            .Where(c => c.UpdatedAt < updatedBeforeUtc && c.ReminderSentAt == null && c.ItemsJson != "[]")
            .ToListAsync(ct);

    public void Add(CartSnapshot snapshot) => _dbContext.CartSnapshots.Add(snapshot);

    public void Remove(CartSnapshot snapshot) => _dbContext.CartSnapshots.Remove(snapshot);
}
