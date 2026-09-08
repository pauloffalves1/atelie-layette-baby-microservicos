namespace AtelieBebe.Orders.Core.Application.Coupons;

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken ct = default);
    Task<CouponDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CouponDto> CreateAsync(CreateCouponRequest request, CancellationToken ct = default);
    Task<CouponDto> SetActiveAsync(Guid id, bool active, CancellationToken ct = default);

    /// <summary>Checked at checkout before the order is submitted, so an invalid code never gets as far as order creation.</summary>
    Task<ValidateCouponResponse> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default);
}
