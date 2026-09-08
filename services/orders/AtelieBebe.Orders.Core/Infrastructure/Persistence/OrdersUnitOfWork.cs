using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Domain.Repositories;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence;

public sealed class OrdersUnitOfWork : IOrdersUnitOfWork
{
    private readonly OrdersDbContext _dbContext;

    public OrdersUnitOfWork(OrdersDbContext dbContext, IOrderRepository orders, ICouponRepository coupons, ICartSnapshotRepository cartSnapshots)
    {
        _dbContext = dbContext;
        Orders = orders;
        Coupons = coupons;
        CartSnapshots = cartSnapshots;
    }

    public IOrderRepository Orders { get; }
    public ICouponRepository Coupons { get; }
    public ICartSnapshotRepository CartSnapshots { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _dbContext.SaveChangesAsync(ct);
}
