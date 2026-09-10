using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Orders.Core.Tests.Domain;

public class CouponTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100)]
    [InlineData(150)]
    public void Create_DiscountOutOfRange_ThrowsDomainException(decimal discount)
    {
        Assert.Throws<DomainException>(() => Coupon.Create("BEMVINDA10", discount, null, null));
    }

    [Fact]
    public void Create_ZeroMaxUses_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Coupon.Create("BEMVINDA10", 10, null, 0));
    }

    [Fact]
    public void Create_NormalizesCodeToUppercaseTrimmed()
    {
        var coupon = Coupon.Create("  bemvinda10  ", 10, null, null);

        Assert.Equal("BEMVINDA10", coupon.Code);
    }

    [Fact]
    public void IsValid_NoExpirationNoMaxUses_IsAlwaysValidWhileActive()
    {
        var coupon = Coupon.Create("BEMVINDA10", 10, null, null);

        Assert.True(coupon.IsValid);
    }

    [Fact]
    public void IsValid_PastExpiration_IsFalse()
    {
        var coupon = Coupon.Create("EXPIRADO", 10, DateTime.UtcNow.AddDays(-1), null);

        Assert.False(coupon.IsValid);
    }

    [Fact]
    public void IsValid_UsesReachedMax_IsFalse()
    {
        var coupon = Coupon.Create("LIMITADO", 10, null, maxUses: 1);
        coupon.RecordUse();

        Assert.False(coupon.IsValid);
    }

    [Fact]
    public void IsValid_Deactivated_IsFalse()
    {
        var coupon = Coupon.Create("DESATIVADO", 10, null, null);
        coupon.SetActive(false);

        Assert.False(coupon.IsValid);
    }

    [Fact]
    public void RecordUse_AlreadyInvalid_ThrowsDomainException()
    {
        var coupon = Coupon.Create("EXPIRADO", 10, DateTime.UtcNow.AddDays(-1), null);

        Assert.Throws<DomainException>(coupon.RecordUse);
    }

    [Fact]
    public void RecordUse_Valid_IncrementsUsesCount()
    {
        var coupon = Coupon.Create("BEMVINDA10", 10, null, null);

        coupon.RecordUse();

        Assert.Equal(1, coupon.UsesCount);
    }

    // --- TDD demo: written first (red), then Coupon.Create was extended to make it pass (green). ---
    // See microservices/README.md ("Estratégia de testes > TDD") for the full red-green-refactor
    // walkthrough this test is part of.
    [Theory]
    [InlineData("BEM VINDA10")]
    [InlineData("BEMVINDA-10")]
    [InlineData("BEMVINDA_10")]
    [InlineData("10%OFF")]
    public void Create_CodeWithSpacesOrSymbols_ThrowsDomainException(string code)
    {
        Assert.Throws<DomainException>(() => Coupon.Create(code, 10, null, null));
    }
}
