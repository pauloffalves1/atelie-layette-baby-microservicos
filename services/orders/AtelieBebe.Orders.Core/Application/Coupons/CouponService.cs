using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Orders.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Orders.Core.Application.Coupons;

public sealed class CouponService : ICouponService
{
    private readonly IOrdersUnitOfWork _unitOfWork;
    private readonly ILogger<CouponService> _logger;

    public CouponService(IOrdersUnitOfWork unitOfWork, ILogger<CouponService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var coupons = await _unitOfWork.Coupons.ListAsync(ct);
            var result = coupons.Select(ToDto).ToList();

            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(CreateAsync));
        try
        {
            if (await _unitOfWork.Coupons.GetByCodeAsync(request.Code, ct) is not null)
                throw new ConflictException("Já existe um cupom com este código.");

            var coupon = Coupon.Create(request.Code, request.DiscountPercentage, request.ExpiresAt, request.MaxUses);
            _unitOfWork.Coupons.Add(coupon);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(CreateAsync));
            return ToDto(coupon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateAsync));
            throw;
        }
    }

    public async Task<CouponDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetByIdAsync));
        try
        {
            var coupon = await _unitOfWork.Coupons.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Cupom", id);

            _logger.LogInformation("Saindo de {Method}", nameof(GetByIdAsync));
            return ToDto(coupon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<CouponDto> SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetActiveAsync));
        try
        {
            var coupon = await _unitOfWork.Coupons.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Cupom", id);

            coupon.SetActive(active);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SetActiveAsync));
            return ToDto(coupon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetActiveAsync));
            throw;
        }
    }

    public async Task<ValidateCouponResponse> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ValidateAsync));
        try
        {
            var coupon = await _unitOfWork.Coupons.GetByCodeAsync(request.Code, ct);
            if (coupon is null || !coupon.IsValid)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(ValidateAsync));
                return new ValidateCouponResponse(false, 0, 0, "Cupom inválido ou expirado.");
            }

            var discountAmount = Math.Round(request.Subtotal * coupon.DiscountPercentage / 100m, 2);

            _logger.LogInformation("Saindo de {Method}", nameof(ValidateAsync));
            return new ValidateCouponResponse(true, coupon.DiscountPercentage, discountAmount, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ValidateAsync));
            throw;
        }
    }

    private static CouponDto ToDto(Coupon c) =>
        new(c.Id, c.Code, c.DiscountPercentage, c.ExpiresAt, c.MaxUses, c.UsesCount, c.Active, c.IsValid, c.CreatedAt);
}
