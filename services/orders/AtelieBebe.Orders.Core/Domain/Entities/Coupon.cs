using System.Text.RegularExpressions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Orders.Core.Domain.Entities;

/// <summary>
/// A discount code a customer types at checkout — independent of the per-product time-boxed
/// promotions (<see cref="Product.SetPromotion"/>), which apply automatically without any code.
/// </summary>
public sealed partial class Coupon : Entity, IAggregateRoot
{
    public string Code { get; private set; } = default!;
    public decimal DiscountPercentage { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int? MaxUses { get; private set; }
    public int UsesCount { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>False once deactivated, expired, or its use limit is reached — checked before every redemption.</summary>
    public bool IsValid =>
        Active &&
        (ExpiresAt is null || DateTime.UtcNow <= ExpiresAt) &&
        (MaxUses is null || UsesCount < MaxUses);

    private Coupon() { } // EF Core

    private Coupon(Guid id, string code, decimal discountPercentage, DateTime? expiresAt, int? maxUses) : base(id)
    {
        Code = code;
        DiscountPercentage = discountPercentage;
        ExpiresAt = expiresAt;
        MaxUses = maxUses;
        UsesCount = 0;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Coupon Create(string code, decimal discountPercentage, DateTime? expiresAt, int? maxUses)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código do cupom é obrigatório.");

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (!AlphanumericCodeRegex().IsMatch(normalizedCode))
            throw new DomainException("O código do cupom deve conter apenas letras e números, sem espaços ou símbolos.");

        if (discountPercentage <= 0 || discountPercentage >= 100)
            throw new DomainException("O desconto deve ser um percentual entre 1 e 99.");
        if (maxUses is <= 0)
            throw new DomainException("O limite de usos, quando informado, deve ser maior que zero.");

        return new Coupon(Guid.NewGuid(), normalizedCode, discountPercentage, expiresAt, maxUses);
    }

    [GeneratedRegex(@"^[A-Z0-9]+$")]
    private static partial Regex AlphanumericCodeRegex();

    public void SetActive(bool active) => Active = active;

    /// <summary>Called once a redemption is confirmed (order created) — throws if the coupon became invalid between validation and redemption.</summary>
    public void RecordUse()
    {
        if (!IsValid)
            throw new DomainException("Este cupom não é mais válido.");

        UsesCount++;
    }
}
