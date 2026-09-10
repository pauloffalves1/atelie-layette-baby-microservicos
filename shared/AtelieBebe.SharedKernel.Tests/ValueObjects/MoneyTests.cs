using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.SharedKernel.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void FromReais_NegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Money.FromReais(-0.01m));
    }

    [Theory]
    [InlineData(10.005, 10.01)]
    [InlineData(10.004, 10.00)]
    public void FromReais_RoundsToTwoDecimalPlacesAwayFromZero(decimal input, decimal expected)
    {
        var money = Money.FromReais(input);

        Assert.Equal(expected, money.Amount);
    }

    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var total = Money.FromReais(10m).Add(Money.FromReais(5.50m));

        Assert.Equal(15.50m, total.Amount);
    }

    [Fact]
    public void Subtract_DiscountLargerThanAmount_ClampsAtZero()
    {
        var result = Money.FromReais(10m).Subtract(Money.FromReais(50m));

        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void Multiply_NegativeFactor_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Money.FromReais(10m).Multiply(-1));
    }

    [Fact]
    public void Multiply_ByQuantity_MultipliesAmount()
    {
        var subtotal = Money.FromReais(19.90m).Multiply(3);

        Assert.Equal(59.70m, subtotal.Amount);
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(Money.FromReais(42m), Money.FromReais(42m));
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsDomainException()
    {
        var reais = Money.FromReais(10m, "BRL");
        var dollars = Money.FromReais(10m, "USD");

        Assert.Throws<DomainException>(() => reais.Add(dollars));
    }
}
