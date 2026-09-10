using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Orders.Core.Tests.Domain;

public class OrderTests
{
    private static Order CreateOrder(OrderType type = OrderType.Loja) =>
        Order.Create(
            customerId: Guid.NewGuid(),
            customerName: "Maria Silva",
            customerEmail: Email.Create("maria@exemplo.com"),
            customerPhone: "11999998888",
            customerCpf: Cpf.Create("11144477735"),
            type: type);

    [Fact]
    public void Create_MissingCustomerName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Order.Create(
            Guid.NewGuid(), "  ", Email.Create("a@a.com"), "11999998888", Cpf.Create("11144477735"), OrderType.Loja));
    }

    [Fact]
    public void Create_MissingCpf_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Order.Create(
            Guid.NewGuid(), "Maria", Email.Create("a@a.com"), "11999998888", null, OrderType.Loja));
    }

    [Fact]
    public void Submit_LojaOrderWithNoItems_ThrowsDomainException()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(order.Submit);
    }

    [Fact]
    public void Submit_CustomOrderWithNoItems_DoesNotThrow()
    {
        var order = CreateOrder(OrderType.Personalizada);

        var exception = Record.Exception(order.Submit);

        Assert.Null(exception);
    }

    [Fact]
    public void AddItem_AfterOrderLeftRecebido_ThrowsDomainException()
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), "Kit ombro e boca", Money.FromReais(89.90m), 1);
        order.Submit();
        order.ChangeStatus(OrderStatus.EmProducao);

        Assert.Throws<DomainException>(() => order.AddItem(Guid.NewGuid(), "Fralda extra", Money.FromReais(29.90m), 1));
    }

    [Fact]
    public void Total_SumsItemsAndShippingMinusCoupon()
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), "Kit ombro e boca", Money.FromReais(100m), 2); // 200
        order.ApplyCoupon("BEMVINDA10", Money.FromReais(20m));

        Assert.Equal(180m, order.Total.Amount);
    }

    [Theory]
    [InlineData(OrderStatus.Recebido, OrderStatus.EmProducao, true)]
    [InlineData(OrderStatus.Recebido, OrderStatus.Cancelado, true)]
    [InlineData(OrderStatus.Recebido, OrderStatus.Enviado, false)]
    [InlineData(OrderStatus.EmProducao, OrderStatus.Pronto, true)]
    [InlineData(OrderStatus.EmProducao, OrderStatus.Recebido, false)]
    [InlineData(OrderStatus.Pronto, OrderStatus.Enviado, true)]
    [InlineData(OrderStatus.Enviado, OrderStatus.Entregue, true)]
    [InlineData(OrderStatus.Enviado, OrderStatus.Cancelado, false)]
    [InlineData(OrderStatus.Entregue, OrderStatus.Cancelado, false)]
    [InlineData(OrderStatus.Cancelado, OrderStatus.Recebido, false)]
    public void ChangeStatus_FollowsAllowedStateMachine(OrderStatus from, OrderStatus to, bool allowed)
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), "Kit ombro e boca", Money.FromReais(50m), 1);
        order.Submit();
        AdvanceTo(order, from);

        if (allowed)
        {
            order.ChangeStatus(to);
            Assert.Equal(to, order.Status);
        }
        else
        {
            Assert.Throws<DomainException>(() => order.ChangeStatus(to));
        }
    }

    /// <summary>Walks the order through the real, valid state-machine path up to (and including) <paramref name="status"/>.</summary>
    private static void AdvanceTo(Order order, OrderStatus status)
    {
        var path = new[] { OrderStatus.EmProducao, OrderStatus.Pronto, OrderStatus.Enviado, OrderStatus.Entregue };

        if (status == OrderStatus.Cancelado)
        {
            order.ChangeStatus(OrderStatus.Cancelado);
            return;
        }

        foreach (var step in path)
        {
            if (order.Status == status) return;
            order.ChangeStatus(step);
        }
    }

    [Fact]
    public void ChangeStatus_SameStatus_IsNoOpAndDoesNotThrow()
    {
        var order = CreateOrder();

        var exception = Record.Exception(() => order.ChangeStatus(OrderStatus.Recebido));

        Assert.Null(exception);
        Assert.Equal(OrderStatus.Recebido, order.Status);
    }

    [Fact]
    public void MarkPaymentApproved_AlreadyPaid_IsIdempotent()
    {
        var order = CreateOrder();
        order.MarkPaymentApproved("pay_1");

        order.MarkPaymentRejected("pay_2");

        Assert.Equal(PaymentStatus.Pago, order.PaymentStatus);
        Assert.Equal("pay_1", order.ExternalPaymentId);
    }
}
