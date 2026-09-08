using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Domain.Events;

public sealed record ContactMessageReceivedDomainEvent(
    Guid MessageId,
    string Name,
    string Email,
    string Phone) : DomainEventBase;
