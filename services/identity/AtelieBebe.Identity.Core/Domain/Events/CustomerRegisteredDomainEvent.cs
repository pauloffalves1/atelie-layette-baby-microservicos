using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Identity.Core.Domain.Events;

public sealed record CustomerRegisteredDomainEvent(
    Guid CustomerId,
    string Name,
    string Email,
    string Phone) : DomainEventBase;
