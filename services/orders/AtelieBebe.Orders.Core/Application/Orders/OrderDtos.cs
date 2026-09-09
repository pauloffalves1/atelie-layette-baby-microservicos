namespace AtelieBebe.Orders.Core.Application.Orders;

public sealed record OrderItemDto(
    Guid Id,
    Guid? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    string? OptionsJson);

public sealed record OrderDto(
    Guid Id,
    Guid? CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string? CustomerCpf,
    string Type,
    string Status,
    decimal ItemsTotal,
    decimal ShippingCost,
    decimal Total,
    string? Notes,
    string? GiftMessage,
    string? CustomDetailsJson,
    string? ShippingAddressJson,
    string DeliveryMethod,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemDto> Items,
    string PaymentStatus,
    string? ExternalPaymentId,
    string? TrackingCode,
    string? CouponCode,
    decimal CouponDiscountAmount,
    string? PaymentDeclineReason = null,
    string? PixQrCodeText = null,
    string? PixQrCodeImageUrl = null);

public sealed record CreateOrderItemRequest(Guid? ProductId, string ProductName, decimal UnitPrice, int Quantity, string? OptionsJson);

/// <summary>PaymentMethod is "CREDIT_CARD" or "PIX". EncryptedCard/Installments only apply to CREDIT_CARD — the card is encrypted client-side via PagBank's JS SDK before it ever reaches us.</summary>
public sealed record CreateStoreOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string CustomerCpf,
    string? Notes,
    string? ShippingAddressJson,
    decimal ShippingCost,
    IReadOnlyList<CreateOrderItemRequest> Items,
    string? CouponCode = null,
    string PaymentMethod = "PIX",
    string? EncryptedCard = null,
    int Installments = 1,
    string? GiftMessage = null,
    string? ThreeDsAuthenticationId = null,
    string DeliveryMethod = "Entrega");

public sealed record CreateCustomOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string CustomerCpf,
    string? Notes,
    string CustomDetailsJson,
    decimal EstimatedPrice);

public sealed record UpdateOrderStatusRequest(string Status);
public sealed record SetTrackingCodeRequest(string? TrackingCode);
