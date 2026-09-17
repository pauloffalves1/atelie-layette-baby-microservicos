namespace AtelieBebe.Orders.Core.Application.Orders;

public sealed record OrderItemDto(
    Guid Id,
    Guid? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    string? OptionsJson,
    string? ModerationFlag);

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
    string? RecipientName,
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
    string? PixQrCodeImageUrl = null,
    string? BoletoBarcode = null,
    string? BoletoUrl = null,
    /// <summary>A test purchase (RF40) — shown only on the test screen, which is the only listing that returns these.</summary>
    bool IsTest = false);

public sealed record CreateOrderItemRequest(Guid? ProductId, string ProductName, decimal UnitPrice, int Quantity, string? OptionsJson);

/// <summary>PaymentMethod is "CREDIT_CARD", "PIX" or "BOLETO". EncryptedCard/Installments only apply to CREDIT_CARD — the card is encrypted client-side via PagBank's JS SDK before it ever reaches us. BOLETO requires ShippingAddressJson (the holder's address is mandatory on PagBank's boleto API), regardless of DeliveryMethod.</summary>
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
    string DeliveryMethod = "Entrega",
    string? RecipientName = null);

public sealed record CreateCustomOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string CustomerCpf,
    string? Notes,
    string CustomDetailsJson,
    decimal EstimatedPrice);

/// <summary>
/// An order closed outside the site (WhatsApp, in person) and typed into the admin panel. Prices are
/// the ones agreed with the customer, so they are taken as sent — unlike the storefront checkout,
/// which always re-reads the catalog price. ProductId is optional (links the item to a catalog
/// product for reviews and reports).
/// </summary>
public sealed record CreateManualOrderRequest(
    Guid? CustomerId,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string CustomerCpf,
    string DeliveryMethod,
    string? ShippingAddressJson,
    decimal ShippingCost,
    IReadOnlyList<CreateOrderItemRequest> Items,
    bool PaymentReceived,
    bool NotifyCustomer,
    string? Notes = null,
    string? GiftMessage = null,
    string? RecipientName = null);

public sealed record UpdateOrderStatusRequest(string Status);
public sealed record SetTrackingCodeRequest(string? TrackingCode);
