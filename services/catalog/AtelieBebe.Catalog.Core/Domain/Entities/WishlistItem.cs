using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>A product a customer favorited. One row per (customer, product) — enforced by a unique index.</summary>
public sealed class WishlistItem : Entity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WishlistItem() { } // EF Core

    private WishlistItem(Guid id, Guid customerId, Guid productId) : base(id)
    {
        CustomerId = customerId;
        ProductId = productId;
        CreatedAt = DateTime.UtcNow;
    }

    public static WishlistItem Create(Guid customerId, Guid productId)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Cliente inválido.");
        if (productId == Guid.Empty)
            throw new DomainException("Produto inválido.");

        return new WishlistItem(Guid.NewGuid(), customerId, productId);
    }
}
