using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Orders.Core.Domain.Events;

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    decimal TotalAmount,
    /// <summary>A test purchase (RF40): the customer's own notifications still go out, the ateliê's "nova encomenda" alert doesn't. Defaulted so outbox rows written before this field existed still deserialize.</summary>
    bool IsTest = false) : DomainEventBase;
