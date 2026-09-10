using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Orders;
using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.Orders.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Reqnroll;

namespace AtelieBebe.Orders.Core.Tests.Features;

[Binding]
public class CancelamentoDePedidoSteps
{
    private readonly IOrdersUnitOfWork _unitOfWork = Substitute.For<IOrdersUnitOfWork>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private Order _order = default!;
    private Guid _ownerCustomerId;
    private OrderDto? _result;
    private Exception? _thrownException;

    public CancelamentoDePedidoSteps()
    {
        _unitOfWork.Orders.Returns(_orderRepository);
    }

    private OrderService BuildService() => new(
        _unitOfWork,
        Substitute.For<IPaymentGateway>(),
        Substitute.For<ICatalogServiceClient>(),
        Substitute.For<ILogger<OrderService>>());

    [Given("que existe um pedido feito por {string} com status {string}")]
    public void DadoQueExisteUmPedidoComStatus(string customerName, string status)
    {
        _ownerCustomerId = Guid.NewGuid();
        _order = Order.Create(_ownerCustomerId, customerName, Email.Create("cliente@exemplo.com"),
            "11999998888", Cpf.Create("11144477735"), OrderType.Loja);
        _order.AddItem(Guid.NewGuid(), "Kit ombro e boca", Money.FromReais(89.90m), 1);
        _order.Submit();

        Assert.Equal(status, _order.Status.ToString());
        _orderRepository.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(_order);
    }

    [Given("que o pedido muda para o status {string}")]
    public void DadoQueOPedidoMudaParaOStatus(string status)
    {
        AdvanceTo(_order, Enum.Parse<OrderStatus>(status));
    }

    [When("a própria cliente pede o cancelamento do pedido")]
    public async Task QuandoAPropriaClientePedeOCancelamento()
    {
        await TryCancelAsync(_ownerCustomerId);
    }

    [When("outra cliente pede o cancelamento do pedido")]
    public async Task QuandoOutraClientePedeOCancelamento()
    {
        await TryCancelAsync(Guid.NewGuid());
    }

    private async Task TryCancelAsync(Guid customerId)
    {
        try
        {
            _result = await BuildService().CancelMyOrderAsync(_order.Id, customerId);
        }
        catch (Exception ex)
        {
            _thrownException = ex;
        }
    }

    [Then("o pedido passa a ter o status {string}")]
    public void EntaoOPedidoPassaATerOStatus(string status)
    {
        Assert.Null(_thrownException);
        Assert.Equal(status, _result?.Status);
    }

    [Then("o cancelamento é recusado com a mensagem {string}")]
    public void EntaoOCancelamentoERecusadoComAMensagem(string message)
    {
        var exception = Assert.IsType<ConflictException>(_thrownException);
        Assert.Equal(message, exception.Message);
    }

    [Then("o cancelamento é recusado por não encontrar o pedido")]
    public void EntaoOCancelamentoERecusadoPorNaoEncontrarOPedido()
    {
        Assert.IsType<NotFoundException>(_thrownException);
    }

    private static void AdvanceTo(Order order, OrderStatus status)
    {
        var path = new[] { OrderStatus.EmProducao, OrderStatus.Pronto, OrderStatus.Enviado, OrderStatus.Entregue };
        foreach (var step in path)
        {
            if (order.Status == status) return;
            order.ChangeStatus(step);
        }
    }
}
