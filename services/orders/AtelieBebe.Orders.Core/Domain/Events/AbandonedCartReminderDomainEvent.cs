using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Orders.Core.Domain.Events;

public sealed record AbandonedCartReminderItem(string ProductName, string ProductUrl, int Quantity);

/// <summary>
/// Carries fully-resolved customer/product names — not just ids — exactly like OrderCreatedDomainEvent
/// does, so Notifications never has to call back into Identity/Catalog to render the e-mail; Orders
/// resolves the enrichment once (via its own internal-service HTTP clients), same trade-off already
/// made everywhere else in this codebase.
/// </summary>
public sealed record AbandonedCartReminderDomainEvent(
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<AbandonedCartReminderItem> Items,
    string ShopUrl) : DomainEventBase;
