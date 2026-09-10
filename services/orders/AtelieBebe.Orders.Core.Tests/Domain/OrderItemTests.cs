using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Orders.Core.Tests.Domain;

/// <summary>
/// OrderItem has no public factory of its own — it's a child entity only ever created through
/// <see cref="Order.AddItem"/>, so its invariants are exercised via the aggregate root, same as
/// production code does.
/// </summary>
public class OrderItemTests
{
    private static Order CreateOrder() =>
        Order.Create(Guid.NewGuid(), "Maria Silva", Email.Create("maria@exemplo.com"), "11999998888",
            Cpf.Create("11144477735"), OrderType.Loja);

    [Fact]
    public void AddItem_ZeroQuantity_ThrowsDomainException()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), "Fralda de boca", Money.FromReais(29.90m), 0));
    }

    [Fact]
    public void AddItem_NegativeQuantity_ThrowsDomainException()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), "Fralda de boca", Money.FromReais(29.90m), -1));
    }

    [Fact]
    public void AddItem_MissingProductName_ThrowsDomainException()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), "", Money.FromReais(29.90m), 1));
    }

    [Fact]
    public void AddItem_Subtotal_MultipliesUnitPriceByQuantity()
    {
        var order = CreateOrder();

        order.AddItem(Guid.NewGuid(), "Fralda de boca", Money.FromReais(29.90m), 3);

        Assert.Equal(89.70m, order.Items.Single().Subtotal.Amount);
    }
}
