using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Orders;
using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.Orders.Core.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AtelieBebe.Orders.Core.Tests.Application;

/// <summary>Admin order search (RF35) — the actual matching is a SQL LIKE in OrderRepository, so
/// these only pin that the service forwards the term (and still parses the other filters) to it.</summary>
public class OrderServiceAdminSearchTests
{
    private readonly IOrdersUnitOfWork _unitOfWork = Substitute.For<IOrdersUnitOfWork>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();

    public OrderServiceAdminSearchTests()
    {
        _unitOfWork.Orders.Returns(_orders);
        _orders.ListAsync(default, default, default, default, default, default)
            .ReturnsForAnyArgs((Array.Empty<Order>(), 0));
        _orders.ListAllAsync(default, default, default, default)
            .ReturnsForAnyArgs(Array.Empty<Order>());
    }

    private OrderService BuildService() => new(
        _unitOfWork,
        Substitute.For<IPaymentGateway>(),
        Substitute.For<ICatalogServiceClient>(),
        Substitute.For<IEmbroideryModerationScreener>(),
        Substitute.For<ILogger<OrderService>>());

    [Fact]
    public async Task ListAsync_ForwardsSearchTermAndParsedFiltersToRepository()
    {
        await BuildService().ListAsync("Recebido", "Pago", page: 2, pageSize: 20, search: "maria");

        await _orders.Received(1).ListAsync(OrderStatus.Recebido, PaymentStatus.Pago, 2, 20, "maria", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportAsync_AppliesTheSameSearchAsTheListing()
    {
        await BuildService().ExportAsync(null, null, search: "#722ee49d");

        await _orders.Received(1).ListAllAsync(null, null, "#722ee49d", Arg.Any<CancellationToken>());
    }
}
