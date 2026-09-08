using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Orders.Core.Domain.Enums;

namespace AtelieBebe.Orders.Core.Domain.Events;

public sealed record OrderStatusChangedDomainEvent(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    OrderStatus OldStatus,
    OrderStatus NewStatus) : DomainEventBase;
