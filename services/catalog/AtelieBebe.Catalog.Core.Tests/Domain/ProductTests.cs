using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Catalog.Core.Tests.Domain;

public class ProductTests
{
    private static Product CreateProduct() =>
        Product.Create("Kit Ombro e Boca Nuvem", "kit-ombro-e-boca-nuvem", "Descrição", Money.FromReais(129.90m), "Kit Ombro e Boca", null);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => Product.Create(name, "slug", null, Money.FromReais(10m), "Fralda de Boca", null));
    }

    [Fact]
    public void Create_NormalizesSlugToLowercaseTrimmed()
    {
        var product = Product.Create("Produto", "  Meu-Slug  ", null, Money.FromReais(10m), "Fralda de Boca", null);

        Assert.Equal("meu-slug", product.Slug);
    }

    [Fact]
    public void HasAccess_PublicProduct_IsVisibleToEveryone()
    {
        var product = CreateProduct();

        Assert.True(product.HasAccess(null));
        Assert.True(product.HasAccess(Guid.NewGuid()));
    }

    [Fact]
    public void HasAccess_ExclusiveProduct_OnlyVisibleToAllowedCustomer()
    {
        var product = CreateProduct();
        var allowedCustomerId = Guid.NewGuid();
        product.SetAllowedCustomers([allowedCustomerId]);

        Assert.True(product.IsExclusive);
        Assert.True(product.HasAccess(allowedCustomerId));
        Assert.False(product.HasAccess(Guid.NewGuid()));
        Assert.False(product.HasAccess(null));
    }

    [Fact]
    public void SetAllowedCustomers_EmptyList_MakesProductPublicAgain()
    {
        var product = CreateProduct();
        product.SetAllowedCustomers([Guid.NewGuid()]);

        product.SetAllowedCustomers([]);

        Assert.False(product.IsExclusive);
        Assert.True(product.HasAccess(null));
    }

    [Fact]
    public void SetPromotion_ClearingAllThreeFields_RemovesPromotion()
    {
        var product = CreateProduct();
        product.SetPromotion(20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        product.SetPromotion(null, null, null);

        Assert.False(product.IsOnPromotion);
        Assert.Equal(product.Price.Amount, product.EffectivePrice.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(100)]
    public void SetPromotion_DiscountOutOfRange_ThrowsDomainException(decimal discount)
    {
        var product = CreateProduct();

        Assert.Throws<DomainException>(() => product.SetPromotion(discount, DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void SetPromotion_EndBeforeStart_ThrowsDomainException()
    {
        var product = CreateProduct();

        Assert.Throws<DomainException>(() => product.SetPromotion(20, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void IsOnPromotion_WithinWindow_IsTrueAndAppliesDiscountToEffectivePrice()
    {
        var product = CreateProduct(); // Price = 129.90
        product.SetPromotion(10, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));

        Assert.True(product.IsOnPromotion);
        Assert.Equal(116.91m, product.EffectivePrice.Amount);
    }

    [Fact]
    public void IsOnPromotion_OutsideWindow_IsFalseAndEffectivePriceIsRegularPrice()
    {
        var product = CreateProduct();
        product.SetPromotion(10, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1));

        Assert.False(product.IsOnPromotion);
        Assert.Equal(product.Price.Amount, product.EffectivePrice.Amount);
    }

    [Fact]
    public void SetActive_ReactivatingInactiveProduct_RaisesBackInStockDomainEvent()
    {
        var product = CreateProduct();
        product.SetActive(false);
        product.ClearDomainEvents();

        product.SetActive(true);

        Assert.Contains(product.DomainEvents, e => e.GetType().Name == "ProductBackInStockDomainEvent");
    }

    [Fact]
    public void SetActive_AlreadyActive_DoesNotRaiseBackInStockDomainEvent()
    {
        var product = CreateProduct();
        product.ClearDomainEvents();

        product.SetActive(true);

        Assert.Empty(product.DomainEvents);
    }
}
