using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.Orders.Core.Domain.Events;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Orders.Core.Tests.Domain;

/// <summary>Test purchases made against the real checkout in production (RF40).</summary>
public class OrderTestPurchaseTests
{
    private static Order CreateOrder() =>
        Order.Create(
            customerId: Guid.NewGuid(),
            customerName: "Paulo Francisco",
            customerEmail: Email.Create("teste@exemplo.com"),
            customerPhone: "11999998888",
            customerCpf: Cpf.Create("11144477735"),
            type: OrderType.Loja);

    [Fact]
    public void NewOrder_IsNotATestPurchase()
    {
        Assert.False(CreateOrder().IsTest);
    }

    [Fact]
    public void MarkAsTest_FlagsTheOrder()
    {
        var order = CreateOrder();

        order.MarkAsTest();

        Assert.True(order.IsTest);
    }

    [Fact]
    public void MarkAsTest_CalledTwice_StaysMarked()
    {
        var order = CreateOrder();

        order.MarkAsTest();
        order.MarkAsTest();

        Assert.True(order.IsTest);
    }

    [Fact]
    public void Submit_TestOrder_CarriesTheFlagOnTheEvent()
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), "Fralda de teste", Money.FromReais(1m), 1);
        order.MarkAsTest();

        order.Submit();

        var created = Assert.IsType<OrderCreatedDomainEvent>(Assert.Single(order.DomainEvents));
        Assert.True(created.IsTest);
    }

    [Fact]
    public void Submit_RegularOrder_LeavesTheFlagOffTheEvent()
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), "Fralda de ombro", Money.FromReais(120m), 1);

        order.Submit();

        var created = Assert.IsType<OrderCreatedDomainEvent>(Assert.Single(order.DomainEvents));
        Assert.False(created.IsTest);
    }
}
