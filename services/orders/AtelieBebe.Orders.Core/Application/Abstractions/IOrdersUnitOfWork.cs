using AtelieBebe.Orders.Core.Domain.Repositories;

namespace AtelieBebe.Orders.Core.Application.Abstractions;

/// <summary>Orders service's own unit of work — orders, coupons, cart snapshots.</summary>
public interface IOrdersUnitOfWork
{
    IOrderRepository Orders { get; }
    ICouponRepository Coupons { get; }
    ICartSnapshotRepository CartSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
