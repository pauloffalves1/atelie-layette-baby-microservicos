using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Orders;
using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AtelieBebe.Orders.Core.Tests.Application;

/// <summary>Orders agreed on WhatsApp and typed into the admin panel.</summary>
public class OrderServiceManualOrderTests
{
    private readonly IOrdersUnitOfWork _unitOfWork = Substitute.For<IOrdersUnitOfWork>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICatalogServiceClient _catalog = Substitute.For<ICatalogServiceClient>();
    private Order? _saved;

    public OrderServiceManualOrderTests()
    {
        _unitOfWork.Orders.Returns(_orders);
        _orders.When(o => o.Add(Arg.Any<Order>())).Do(call => _saved = call.Arg<Order>());
    }

    private OrderService BuildService() => new(
        _unitOfWork,
        Substitute.For<IPaymentGateway>(),
        _catalog,
        Substitute.For<IEmbroideryModerationScreener>(),
        Substitute.For<ILogger<OrderService>>());

    private static CreateManualOrderRequest Request(
        bool paymentReceived = false, bool notify = false, string delivery = "Entrega", string? address = "{\"street\":\"Rua A\"}",
        IReadOnlyList<CreateOrderItemRequest>? items = null) => new(
        CustomerId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CustomerName: "Maria Silva",
        CustomerEmail: "maria@exemplo.com",
        CustomerPhone: "(11) 91234-5678",
        CustomerCpf: "111.444.777-35",
        DeliveryMethod: delivery,
        ShippingAddressJson: address,
        ShippingCost: 20m,
        Items: items ?? [new CreateOrderItemRequest(Guid.NewGuid(), "Fralda de Boca Florzinha", 30m, 2, "{\"embroideryText\":\"ANA\"}")],
        PaymentReceived: paymentReceived,
        NotifyCustomer: notify,
        Notes: "Combinado pelo WhatsApp");

    [Fact]
    public async Task CreateManualOrderAsync_KeepsAgreedPricesLinksCustomerAndDoesNotChargeOrCallCatalog()
    {
        var dto = await BuildService().CreateManualOrderAsync(Request());

        Assert.NotNull(_saved);
        Assert.Equal(80m, dto.Total); // 2 × 30 + 20 frete, at the price typed by the admin
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), dto.CustomerId);
        Assert.Equal("Pendente", dto.PaymentStatus);
        Assert.Equal("Recebido", dto.Status);
        Assert.Empty(_saved!.DomainEvents);
        await _catalog.DidNotReceiveWithAnyArgs().GetProductAsync(default, default);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManualOrderAsync_PaidAndNotified_MarksPaidAndRaisesCreationEvent()
    {
        var dto = await BuildService().CreateManualOrderAsync(Request(paymentReceived: true, notify: true));

        Assert.Equal("Pago", dto.PaymentStatus);
        Assert.Equal(OrderService.ManualPaymentReference, dto.ExternalPaymentId);
        Assert.Contains(_saved!.DomainEvents, e => e.GetType().Name == "OrderCreatedDomainEvent");
    }

    [Fact]
    public async Task CreateManualOrderAsync_PickupIgnoresAddressAndShipping()
    {
        var dto = await BuildService().CreateManualOrderAsync(Request(delivery: "Retirada", address: null));

        Assert.Equal("Retirada", dto.DeliveryMethod);
        Assert.Equal(0m, dto.ShippingCost);
        Assert.Null(dto.ShippingAddressJson);
    }

    [Fact]
    public async Task CreateManualOrderAsync_RejectsMissingAddressNoItemsOrNegativePrices()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateManualOrderAsync(Request(address: " ")));
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateManualOrderAsync(Request(items: [])));
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateManualOrderAsync(
            Request(items: [new CreateOrderItemRequest(null, "Item", -1m, 1, null)])));
        _orders.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
