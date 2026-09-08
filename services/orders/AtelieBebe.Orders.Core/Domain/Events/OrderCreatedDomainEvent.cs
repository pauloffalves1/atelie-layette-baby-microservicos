using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Orders.Core.Domain.Events;

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    decimal TotalAmount) : DomainEventBase;
