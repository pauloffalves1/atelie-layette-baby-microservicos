using AtelieBebe.Orders.Core.Domain.Entities;

namespace AtelieBebe.Orders.Core.Domain.Repositories;

public interface ICartSnapshotRepository
{
    Task<CartSnapshot?> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Snapshots last updated before the cutoff, still non-empty, that haven't had a reminder sent yet.</summary>
    Task<IReadOnlyList<CartSnapshot>> ListAbandonedAsync(DateTime updatedBeforeUtc, CancellationToken ct = default);

    void Add(CartSnapshot snapshot);
    void Remove(CartSnapshot snapshot);
}
