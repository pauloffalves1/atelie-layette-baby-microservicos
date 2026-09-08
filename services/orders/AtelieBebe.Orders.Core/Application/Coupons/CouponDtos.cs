namespace AtelieBebe.Orders.Core.Application.Coupons;

public sealed record CouponDto(
    Guid Id, string Code, decimal DiscountPercentage, DateTime? ExpiresAt,
    int? MaxUses, int UsesCount, bool Active, bool IsValid, DateTime CreatedAt);

public sealed record CreateCouponRequest(string Code, decimal DiscountPercentage, DateTime? ExpiresAt, int? MaxUses);

public sealed record ValidateCouponRequest(string Code, decimal Subtotal);

public sealed record ValidateCouponResponse(bool Valid, decimal DiscountPercentage, decimal DiscountAmount, string? Error);
