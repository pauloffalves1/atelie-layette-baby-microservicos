using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Catalog.Core.Domain.Events;

public sealed record WishlistReminderDomainEvent(
    string CustomerName,
    string CustomerEmail,
    string ProductName,
    string ProductUrl) : DomainEventBase;
