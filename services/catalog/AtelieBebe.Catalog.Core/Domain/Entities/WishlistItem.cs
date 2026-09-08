using AtelieBebe.Catalog.Core.Domain.Events;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Catalog.Core.Domain.Entities;

/// <summary>A product a customer favorited. One row per (customer, product) — enforced by a unique index.</summary>
public sealed class WishlistItem : Entity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReminderSentAt { get; private set; }

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

    /// <summary>
    /// Raises the reminder event with fully-resolved names (not just ids) — the caller (the
    /// wishlist-reminder background job) already had to call out to Identity for the anonymized
    /// check and to Catalog's own Products for the active check, so it's passed in here rather
    /// than re-fetched. One domain event per wishlist item, unlike the abandoned-cart reminder
    /// which batches — each wishlisted product gets its own reminder e-mail.
    /// </summary>
    public void MarkReminderSent(string customerName, string customerEmail, string productName, string productUrl)
    {
        ReminderSentAt = DateTime.UtcNow;
        AddDomainEvent(new WishlistReminderDomainEvent(customerName, customerEmail, productName, productUrl));
    }
}
