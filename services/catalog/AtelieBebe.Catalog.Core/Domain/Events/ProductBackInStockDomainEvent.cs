using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Catalog.Core.Domain.Events;

public sealed record ProductBackInStockDomainEvent(
    Guid ProductId,
    string ProductName,
    string ProductSlug) : DomainEventBase;
