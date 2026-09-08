using AtelieBebe.Orders.Core.Domain.Entities;

namespace AtelieBebe.Orders.Core.Domain.Repositories;

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Coupon>> ListAsync(CancellationToken ct = default);
    void Add(Coupon coupon);
}
