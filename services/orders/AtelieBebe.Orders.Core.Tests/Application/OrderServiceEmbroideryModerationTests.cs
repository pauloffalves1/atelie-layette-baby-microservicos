using System.Text.Json;
using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Orders;
using AtelieBebe.Orders.Core.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AtelieBebe.Orders.Core.Tests.Application;

/// <summary>Covers the embroidery-text pre-screen wired into checkout (Requisito 21, RF30) —
/// the screener is an application-layer collaborator (<see cref="IEmbroideryModerationScreener"/>),
/// so these tests mock it rather than exercising the real Anthropic call.</summary>
public class OrderServiceEmbroideryModerationTests
{
    private readonly IOrdersUnitOfWork _unitOfWork = Substitute.For<IOrdersUnitOfWork>();
    private readonly IEmbroideryModerationScreener _screener = Substitute.For<IEmbroideryModerationScreener>();

    public OrderServiceEmbroideryModerationTests()
    {
        _unitOfWork.Orders.Returns(Substitute.For<IOrderRepository>());
    }

    // IPaymentGateway.IsConfigured defaults to false on a bare substitute, so CreateStoreOrderAsync
    // never touches the payment branch — exactly what these moderation-focused tests want to avoid.
    private OrderService BuildService() => new(
        _unitOfWork,
        Substitute.For<IPaymentGateway>(),
        Substitute.For<ICatalogServiceClient>(),
        _screener,
        Substitute.For<ILogger<OrderService>>());

    private static CreateStoreOrderRequest BuildRequest(string? optionsJson) => new(
        CustomerName: "Maria Cliente",
        CustomerEmail: "maria@exemplo.com",
        CustomerPhone: "11999998888",
        CustomerCpf: "11144477735",
        Notes: null,
        ShippingAddressJson: null,
        ShippingCost: 0m,
        Items: [new CreateOrderItemRequest(null, "Fralda de Ombro", 49.90m, 1, optionsJson)],
        DeliveryMethod: "Retirada"); // avoids the "Entrega precisa de endereço" check unrelated to this test

    [Fact]
    public async Task CreateStoreOrderAsync_ScreenerFlagsEmbroideryText_SetsModerationFlagOnItem()
    {
        var optionsJson = JsonSerializer.Serialize(new { embroideryText = "texto suspeito", threadColor = "Rosa" });
        _screener.ScreenAsync("texto suspeito", Arg.Any<CancellationToken>()).Returns("agressivo");

        var dto = await BuildService().CreateStoreOrderAsync(BuildRequest(optionsJson), customerId: null);

        Assert.Equal("agressivo", Assert.Single(dto.Items).ModerationFlag);
    }

    [Fact]
    public async Task CreateStoreOrderAsync_ScreenerFindsNothing_LeavesModerationFlagNull()
    {
        var optionsJson = JsonSerializer.Serialize(new { embroideryText = "Maria", threadColor = "Rosa" });
        _screener.ScreenAsync("Maria", Arg.Any<CancellationToken>()).Returns((string?)null);

        var dto = await BuildService().CreateStoreOrderAsync(BuildRequest(optionsJson), customerId: null);

        Assert.Null(Assert.Single(dto.Items).ModerationFlag);
    }

    [Fact]
    public async Task CreateStoreOrderAsync_NoEmbroideryTextInOptions_NeverCallsScreener()
    {
        var optionsJson = JsonSerializer.Serialize(new { threadColor = "Rosa" }); // no embroideryText field

        var dto = await BuildService().CreateStoreOrderAsync(BuildRequest(optionsJson), customerId: null);

        Assert.Null(Assert.Single(dto.Items).ModerationFlag);
        await _screener.DidNotReceive().ScreenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateStoreOrderAsync_NoOptionsJson_NeverCallsScreener()
    {
        var dto = await BuildService().CreateStoreOrderAsync(BuildRequest(optionsJson: null), customerId: null);

        Assert.Null(Assert.Single(dto.Items).ModerationFlag);
        await _screener.DidNotReceive().ScreenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateStoreOrderAsync_MalformedOptionsJson_NeverCallsScreenerAndStillCreatesOrder()
    {
        var dto = await BuildService().CreateStoreOrderAsync(BuildRequest(optionsJson: "{not valid json"), customerId: null);

        Assert.Null(Assert.Single(dto.Items).ModerationFlag);
        await _screener.DidNotReceive().ScreenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
