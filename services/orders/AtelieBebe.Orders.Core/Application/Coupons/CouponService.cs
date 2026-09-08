using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Orders.Core.Domain.Entities;

namespace AtelieBebe.Orders.Core.Application.Coupons;

public sealed class CouponService : ICouponService
{
    private readonly IOrdersUnitOfWork _unitOfWork;

    public CouponService(IOrdersUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken ct = default)
    {
        var coupons = await _unitOfWork.Coupons.ListAsync(ct);
        return coupons.Select(ToDto).ToList();
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request, CancellationToken ct = default)
    {
        if (await _unitOfWork.Coupons.GetByCodeAsync(request.Code, ct) is not null)
            throw new ConflictException("Já existe um cupom com este código.");

        var coupon = Coupon.Create(request.Code, request.DiscountPercentage, request.ExpiresAt, request.MaxUses);
        _unitOfWork.Coupons.Add(coupon);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(coupon);
    }

    public async Task<CouponDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await _unitOfWork.Coupons.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cupom", id);
        return ToDto(coupon);
    }

    public async Task<CouponDto> SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var coupon = await _unitOfWork.Coupons.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cupom", id);

        coupon.SetActive(active);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(coupon);
    }

    public async Task<ValidateCouponResponse> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default)
    {
        var coupon = await _unitOfWork.Coupons.GetByCodeAsync(request.Code, ct);
        if (coupon is null || !coupon.IsValid)
            return new ValidateCouponResponse(false, 0, 0, "Cupom inválido ou expirado.");

        var discountAmount = Math.Round(request.Subtotal * coupon.DiscountPercentage / 100m, 2);
        return new ValidateCouponResponse(true, coupon.DiscountPercentage, discountAmount, null);
    }

    private static CouponDto ToDto(Coupon c) =>
        new(c.Id, c.Code, c.DiscountPercentage, c.ExpiresAt, c.MaxUses, c.UsesCount, c.Active, c.IsValid, c.CreatedAt);
}
