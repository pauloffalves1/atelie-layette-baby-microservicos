using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Orders.Core.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IOrdersUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrdersUnitOfWork unitOfWork, IPaymentGateway paymentGateway, ICatalogServiceClient catalogServiceClient, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _catalogServiceClient = catalogServiceClient;
        _logger = logger;
    }

    public async Task<OrderDto> CreateStoreOrderAsync(CreateStoreOrderRequest request, Guid? customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(CreateStoreOrderAsync));
        try
        {
            if (request.Items.Count == 0)
                throw new ConflictException("O pedido precisa ter pelo menos um item.");

            if (request.DeliveryMethod == "Entrega" && string.IsNullOrWhiteSpace(request.ShippingAddressJson))
                throw new ConflictException("O endereço de entrega é obrigatório.");

            var order = Order.Create(
                customerId,
                request.CustomerName,
                Email.Create(request.CustomerEmail),
                request.CustomerPhone,
                Cpf.Create(request.CustomerCpf),
                OrderType.Loja,
                request.Notes,
                customDetailsJson: null,
                request.ShippingAddressJson,
                Money.FromReais(request.ShippingCost),
                request.GiftMessage,
                request.DeliveryMethod);

            foreach (var itemRequest in request.Items)
            {
                if (itemRequest.ProductId is Guid productId)
                {
                    var product = await _catalogServiceClient.GetProductAsync(productId, ct)
                        ?? throw new NotFoundException("Produto", productId);

                    order.AddItem(product.Id, product.Name, Money.FromReais(product.EffectivePrice), itemRequest.Quantity, itemRequest.OptionsJson);
                }
                else
                {
                    order.AddItem(null, itemRequest.ProductName, Money.FromReais(itemRequest.UnitPrice), itemRequest.Quantity, itemRequest.OptionsJson);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var coupon = await _unitOfWork.Coupons.GetByCodeAsync(request.CouponCode, ct);
                if (coupon is null || !coupon.IsValid)
                    throw new ConflictException("Cupom inválido ou expirado.");

                var discount = Money.FromReais(Math.Round(order.ItemsTotal.Amount * coupon.DiscountPercentage / 100m, 2));
                order.ApplyCoupon(coupon.Code, discount);
                coupon.RecordUse();
            }

            order.Submit();
            _unitOfWork.Orders.Add(order);
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = ToDto(order);
            string? declineReason = null;
            string? pixQrCodeImageUrl = null;

            if (_paymentGateway.IsConfigured)
            {
                if (string.Equals(request.PaymentMethod, "CREDIT_CARD", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(request.EncryptedCard))
                        throw new ConflictException("Dados do cartão inválidos. Tente novamente.");

                    var charge = await _paymentGateway.ChargeCardAsync(
                        order.Id, "Pedido Ateliê Layette Baby", order.Total.Amount,
                        order.CustomerName, order.CustomerEmail.Value, order.CustomerCpf!.Value, order.CustomerPhone,
                        request.EncryptedCard, request.Installments, request.ThreeDsAuthenticationId, ct);

                    if (charge is null)
                    {
                        throw new ConflictException(
                            "Não foi possível gerar o pagamento online agora. Tente novamente em instantes ou fale " +
                            "conosco pelo WhatsApp para finalizar sua encomenda.");
                    }

                    if (charge.Approved)
                        order.MarkPaymentApproved(charge.ExternalId!);
                    else
                        order.MarkPaymentRejected(charge.ExternalId);

                    declineReason = charge.DeclineReason;
                }
                else
                {
                    var pix = await _paymentGateway.CreatePixChargeAsync(
                        order.Id, "Pedido Ateliê Layette Baby", order.Total.Amount,
                        order.CustomerName, order.CustomerEmail.Value, order.CustomerCpf!.Value, order.CustomerPhone, ct);

                    if (pix is null)
                    {
                        throw new ConflictException(
                            "Não foi possível gerar o pagamento online agora. Tente novamente em instantes ou fale " +
                            "conosco pelo WhatsApp para finalizar sua encomenda.");
                    }

                    order.SetPixCharge(pix.ExternalId, pix.QrCodeText);
                    pixQrCodeImageUrl = pix.QrCodeImageUrl;
                }

                await _unitOfWork.SaveChangesAsync(ct);
                dto = ToDto(order) with { PaymentDeclineReason = declineReason, PixQrCodeImageUrl = pixQrCodeImageUrl };
            }

            _logger.LogInformation("Saindo de {Method}", nameof(CreateStoreOrderAsync));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateStoreOrderAsync));
            throw;
        }
    }

    public async Task<OrderDto> CreateCustomOrderAsync(CreateCustomOrderRequest request, Guid? customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(CreateCustomOrderAsync));
        try
        {
            var order = Order.Create(
                customerId,
                request.CustomerName,
                Email.Create(request.CustomerEmail),
                request.CustomerPhone,
                Cpf.Create(request.CustomerCpf),
                OrderType.Personalizada,
                request.Notes,
                request.CustomDetailsJson);

            order.AddItem(null, "Encomenda personalizada", Money.FromReais(request.EstimatedPrice), 1);
            order.Submit();

            _unitOfWork.Orders.Add(order);
            await _unitOfWork.SaveChangesAsync(ct);

            var result = await GetByIdAsync(order.Id, ct);

            _logger.LogInformation("Saindo de {Method}", nameof(CreateCustomOrderAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateCustomOrderAsync));
            throw;
        }
    }

    public async Task<PagedResult<OrderDto>> ListAsync(string? status, string? paymentStatus, int page, int pageSize, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var (parsedStatus, parsedPaymentStatus) = ParseFilters(status, paymentStatus);

            var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);
            var (orders, totalItems) = await _unitOfWork.Orders.ListAsync(parsedStatus, parsedPaymentStatus, normalizedPage, normalizedPageSize, ct);
            var result = new PagedResult<OrderDto>(orders.Select(ToDto).ToList(), normalizedPage, normalizedPageSize, totalItems);

            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderDto>> ExportAsync(string? status, string? paymentStatus, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ExportAsync));
        try
        {
            var (parsedStatus, parsedPaymentStatus) = ParseFilters(status, paymentStatus);
            var orders = await _unitOfWork.Orders.ListAllAsync(parsedStatus, parsedPaymentStatus, ct);
            var result = orders.Select(ToDto).ToList();

            _logger.LogInformation("Saindo de {Method}", nameof(ExportAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ExportAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderDto>> ListMineAsync(Guid customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListMineAsync));
        try
        {
            var orders = await _unitOfWork.Orders.ListByCustomerAsync(customerId, ct);
            var result = orders.Select(ToDto).OrderByDescending(o => o.CreatedAt).ToList();

            _logger.LogInformation("Saindo de {Method}", nameof(ListMineAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListMineAsync));
            throw;
        }
    }

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetByIdAsync));
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Pedido", id);

            _logger.LogInformation("Saindo de {Method}", nameof(GetByIdAsync));
            return ToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<OrderDto> LookupAsync(string shortId, string email, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(LookupAsync));
        try
        {
            var order = await _unitOfWork.Orders.GetByShortIdAndEmailAsync(shortId, email, ct)
                ?? throw new NotFoundException("Pedido", shortId);

            _logger.LogInformation("Saindo de {Method}", nameof(LookupAsync));
            return ToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(LookupAsync));
            throw;
        }
    }

    public async Task<OrderDto> ChangeStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ChangeStatusAsync));
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Pedido", id);

            order.ChangeStatus(ParseStatus(request.Status));
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(ChangeStatusAsync));
            return ToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ChangeStatusAsync));
            throw;
        }
    }

    public async Task HandlePaymentWebhookAsync(string paymentId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(HandlePaymentWebhookAsync));
        try
        {
            var details = await _paymentGateway.GetPaymentAsync(paymentId, ct);
            if (details is null || string.IsNullOrWhiteSpace(details.ExternalReference))
            {
                _logger.LogInformation("Saindo de {Method}", nameof(HandlePaymentWebhookAsync));
                return;
            }

            if (!Guid.TryParse(details.ExternalReference, out var orderId))
            {
                _logger.LogInformation("Saindo de {Method}", nameof(HandlePaymentWebhookAsync));
                return;
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
            if (order is null)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(HandlePaymentWebhookAsync));
                return;
            }

            switch (details.Status)
            {
                case "approved":
                    order.MarkPaymentApproved(paymentId);
                    break;
                case "rejected":
                case "cancelled":
                    order.MarkPaymentRejected(paymentId);
                    break;
                default:
                    _logger.LogInformation("Saindo de {Method}", nameof(HandlePaymentWebhookAsync));
                    return;
            }

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(HandlePaymentWebhookAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(HandlePaymentWebhookAsync));
            throw;
        }
    }

    /// <summary>Admin-triggered payment retry — generates a fresh PIX charge (no card data to collect on this side) and returns its copy-paste code, e.g. to send the customer via WhatsApp.</summary>
    public async Task<string> GeneratePixChargeAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GeneratePixChargeAsync));
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct)
                ?? throw new NotFoundException("Pedido", orderId);

            if (order.PaymentStatus == PaymentStatus.Pago)
                throw new ConflictException("Este pedido já está pago — não é necessário gerar uma nova cobrança.");

            if (!_paymentGateway.IsConfigured)
                throw new ConflictException("O meio de pagamento online ainda não foi configurado.");

            var pix = await _paymentGateway.CreatePixChargeAsync(
                order.Id, "Pedido Ateliê Layette Baby", order.Total.Amount,
                order.CustomerName, order.CustomerEmail.Value, order.CustomerCpf!.Value, order.CustomerPhone, ct);

            if (pix is null)
                throw new ConflictException("Não foi possível gerar a cobrança PIX agora. Tente novamente em instantes.");

            order.SetPixCharge(pix.ExternalId, pix.QrCodeText);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(GeneratePixChargeAsync));
            return pix.QrCodeText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GeneratePixChargeAsync));
            throw;
        }
    }

    public async Task<OrderDto> SetTrackingCodeAsync(Guid orderId, SetTrackingCodeRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetTrackingCodeAsync));
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct)
                ?? throw new NotFoundException("Pedido", orderId);

            order.SetTrackingCode(request.TrackingCode);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SetTrackingCodeAsync));
            return ToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetTrackingCodeAsync));
            throw;
        }
    }

    public async Task<OrderDto> SimulatePaymentAsync(Guid orderId, bool approved, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SimulatePaymentAsync));
        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct)
                ?? throw new NotFoundException("Pedido", orderId);

            if (approved)
                order.MarkPaymentApproved($"FAKE-{orderId}");
            else
                order.MarkPaymentRejected($"FAKE-{orderId}");

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SimulatePaymentAsync));
            return ToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SimulatePaymentAsync));
            throw;
        }
    }

    private static OrderStatus ParseStatus(string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var parsed))
            throw new ConflictException($"Status de pedido inválido: '{status}'.");
        return parsed;
    }

    private static (OrderStatus? Status, PaymentStatus? PaymentStatus) ParseFilters(string? status, string? paymentStatus)
    {
        OrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
            parsedStatus = ParseStatus(status);

        PaymentStatus? parsedPaymentStatus = null;
        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            if (!Enum.TryParse<PaymentStatus>(paymentStatus, true, out var parsed))
                throw new ConflictException($"Status de pagamento inválido: '{paymentStatus}'.");
            parsedPaymentStatus = parsed;
        }

        return (parsedStatus, parsedPaymentStatus);
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id,
        o.CustomerId,
        o.CustomerName,
        o.CustomerEmail.Value,
        o.CustomerPhone,
        o.CustomerCpf?.Value,
        o.Type.ToString(),
        o.Status.ToString(),
        o.ItemsTotal.Amount,
        o.ShippingCost.Amount,
        o.Total.Amount,
        o.Notes,
        o.GiftMessage,
        o.CustomDetailsJson,
        o.ShippingAddressJson,
        o.DeliveryMethod,
        o.CreatedAt,
        o.UpdatedAt,
        o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount, i.OptionsJson)).ToList(),
        o.PaymentStatus.ToString(),
        o.ExternalPaymentId,
        o.TrackingCode,
        o.CouponCode,
        o.CouponDiscountAmount.Amount,
        PixQrCodeText: o.PixQrCodeText);
}
