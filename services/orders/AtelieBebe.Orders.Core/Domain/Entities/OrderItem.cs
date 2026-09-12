using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Orders.Core.Domain.Entities;

/// <summary>Child entity of the <see cref="Order"/> aggregate. Never accessed outside it.</summary>
public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public Money UnitPrice { get; private set; } = default!;
    public int Quantity { get; private set; }
    public string? OptionsJson { get; private set; }

    /// <summary>Set by an automated pre-screen of the embroidery text in <see cref="OptionsJson"/>
    /// ("ofensivo", "dado_pessoal") to flag the item for manual review before production starts.
    /// Never blocks the order — production staff still decide.</summary>
    public string? ModerationFlag { get; private set; }

    public Money Subtotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { } // EF Core

    internal OrderItem(Guid? productId, string productName, Money unitPrice, int quantity, string? optionsJson, string? moderationFlag = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("O nome do item do pedido é obrigatório.");
        if (quantity <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        OptionsJson = optionsJson;
        ModerationFlag = moderationFlag;
    }

    internal void AttachToOrder(Guid orderId) => OrderId = orderId;
}
