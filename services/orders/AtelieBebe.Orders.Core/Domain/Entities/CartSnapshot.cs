using AtelieBebe.Orders.Core.Domain.Events;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Orders.Core.Domain.Entities;

/// <summary>
/// A server-side copy of a logged-in customer's cart, pushed by the client on every change —
/// only so an abandoned-cart reminder can be sent; the cart itself still lives client-side
/// (localStorage) and this is never read back into the UI. One row per customer (upsert).
/// </summary>
public sealed class CartSnapshot : Entity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string ItemsJson { get; private set; } = default!;
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ReminderSentAt { get; private set; }

    private CartSnapshot() { } // EF Core

    private CartSnapshot(Guid id, Guid customerId, string itemsJson) : base(id)
    {
        CustomerId = customerId;
        ItemsJson = itemsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public static CartSnapshot Create(Guid customerId, string itemsJson)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Cliente inválido.");

        return new CartSnapshot(Guid.NewGuid(), customerId, itemsJson);
    }

    /// <summary>Replaces the item list and resets the reminder — fresh activity means it's no longer abandoned.</summary>
    public void ReplaceItems(string itemsJson)
    {
        ItemsJson = itemsJson;
        UpdatedAt = DateTime.UtcNow;
        ReminderSentAt = null;
    }

    /// <summary>
    /// Raises the reminder event with fully-resolved names (not just ids) — the caller (the
    /// abandoned-cart background job) already had to call out to Identity/Catalog to get this data
    /// for the "no items left" skip check, so it's passed in here rather than re-fetched.
    /// </summary>
    public void MarkReminderSent(string customerName, string customerEmail, IReadOnlyList<AbandonedCartReminderItem> items, string shopUrl)
    {
        ReminderSentAt = DateTime.UtcNow;
        if (items.Count > 0)
            AddDomainEvent(new AbandonedCartReminderDomainEvent(customerName, customerEmail, items, shopUrl));
    }
}
