using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.Orders.Core.Domain.Events;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Orders.Core.Domain.Entities;

public sealed class Order : Entity, IAggregateRoot
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Recebido] = new[] { OrderStatus.EmProducao, OrderStatus.Cancelado },
        [OrderStatus.EmProducao] = new[] { OrderStatus.Pronto, OrderStatus.Cancelado },
        [OrderStatus.Pronto] = new[] { OrderStatus.Enviado, OrderStatus.Cancelado },
        [OrderStatus.Enviado] = new[] { OrderStatus.Entregue },
        [OrderStatus.Entregue] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelado] = Array.Empty<OrderStatus>(),
    };

    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public Email CustomerEmail { get; private set; } = default!;
    public string? CustomerPhone { get; private set; }
    public Cpf? CustomerCpf { get; private set; }
    public OrderType Type { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? GiftMessage { get; private set; }
    public string? CustomDetailsJson { get; private set; }
    public string? ShippingAddressJson { get; private set; }
    public Money ShippingCost { get; private set; } = Money.Zero();
    /// <summary>"Entrega" (delivered, has a ShippingAddressJson + possibly a ShippingCost) or "Retirada" (picked up at the ateliê — ShippingAddressJson stays null, ShippingCost is always zero).</summary>
    public string DeliveryMethod { get; private set; } = "Entrega";
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pendente;
    public string? ExternalPaymentId { get; private set; }
    /// <summary>PIX copy-paste code, persisted so it can still be shown if the customer reloads the confirmation page before scanning it.</summary>
    public string? PixQrCodeText { get; private set; }
    public string? TrackingCode { get; private set; }
    public string? CouponCode { get; private set; }
    public Money CouponDiscountAmount { get; private set; } = Money.Zero();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money ItemsTotal => _items.Aggregate(Money.Zero(), (acc, item) => acc.Add(item.Subtotal));
    public Money Total => ItemsTotal.Add(ShippingCost).Subtract(CouponDiscountAmount);

    private Order() { } // EF Core

    private Order(Guid id, Guid? customerId, string customerName, Email customerEmail, string? customerPhone,
        Cpf? customerCpf, OrderType type, string? notes, string? customDetailsJson, string? shippingAddressJson,
        Money shippingCost, string? giftMessage, string deliveryMethod) : base(id)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        CustomerCpf = customerCpf;
        Type = type;
        Status = OrderStatus.Recebido;
        Notes = notes;
        CustomDetailsJson = customDetailsJson;
        DeliveryMethod = deliveryMethod == "Retirada" ? "Retirada" : "Entrega";
        // Pickup never has a shipping address or cost, regardless of what was passed in — the same
        // "don't trust the client for money-affecting fields" reasoning as ShippingCost below.
        ShippingAddressJson = DeliveryMethod == "Retirada" ? null : shippingAddressJson;
        ShippingCost = DeliveryMethod == "Retirada" ? Money.Zero() : shippingCost;
        GiftMessage = string.IsNullOrWhiteSpace(giftMessage) ? null : giftMessage.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Order Create(Guid? customerId, string customerName, Email customerEmail, string? customerPhone,
        Cpf? customerCpf, OrderType type, string? notes = null, string? customDetailsJson = null, string? shippingAddressJson = null,
        Money? shippingCost = null, string? giftMessage = null, string deliveryMethod = "Entrega")
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("O nome do cliente é obrigatório.");
        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new DomainException("O telefone/WhatsApp é obrigatório.");
        if (customerCpf is null)
            throw new DomainException("O CPF é obrigatório.");

        return new Order(Guid.NewGuid(), customerId, customerName.Trim(), customerEmail, customerPhone.Trim(),
            customerCpf, type, notes, customDetailsJson, shippingAddressJson, shippingCost ?? Money.Zero(), giftMessage, deliveryMethod);
    }

    public void AddItem(Guid? productId, string productName, Money unitPrice, int quantity, string? optionsJson = null)
    {
        if (Status != OrderStatus.Recebido)
            throw new DomainException("Não é possível alterar itens de um pedido que já está em processamento.");

        var item = new OrderItem(productId, productName, unitPrice, quantity, optionsJson);
        item.AttachToOrder(Id);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Confirms the order after all items were added, raising the creation event with the final total.</summary>
    public void Submit()
    {
        if (_items.Count == 0 && Type == OrderType.Loja)
            throw new DomainException("O pedido precisa ter pelo menos um item.");

        AddDomainEvent(new OrderCreatedDomainEvent(Id, CustomerName, CustomerEmail.Value, CustomerPhone!, Total.Amount));
    }

    /// <summary>Idempotent — a payment confirmed as paid is never downgraded by a later/duplicate notification.</summary>
    public void MarkPaymentApproved(string externalPaymentId)
    {
        if (PaymentStatus == PaymentStatus.Pago) return;

        PaymentStatus = PaymentStatus.Pago;
        ExternalPaymentId = externalPaymentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentRejected(string? externalPaymentId)
    {
        if (PaymentStatus == PaymentStatus.Pago) return;

        PaymentStatus = PaymentStatus.Recusado;
        ExternalPaymentId = externalPaymentId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Records a freshly generated PIX charge so its QR code survives a page reload until the webhook confirms payment.</summary>
    public void SetPixCharge(string externalPaymentId, string qrCodeText)
    {
        ExternalPaymentId = externalPaymentId;
        PixQrCodeText = qrCodeText;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Applies a coupon's discount to this order — the caller (Application layer) already validated the coupon and computed the discount amount from <see cref="ItemsTotal"/>.</summary>
    public void ApplyCoupon(string code, Money discountAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código do cupom é obrigatório.");

        CouponCode = code.Trim().ToUpperInvariant();
        CouponDiscountAmount = discountAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Empty/whitespace clears the code — kept free-form (Correios and private couriers use different formats).</summary>
    public void SetTrackingCode(string? trackingCode)
    {
        TrackingCode = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        if (Status == newStatus) return;

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new DomainException($"Não é possível mudar o status de '{Status}' para '{newStatus}'.");

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, CustomerName, CustomerEmail.Value, CustomerPhone!, oldStatus, newStatus));
    }
}
