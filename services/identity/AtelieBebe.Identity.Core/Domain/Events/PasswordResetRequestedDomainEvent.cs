using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Identity.Core.Domain.Events;

public sealed record PasswordResetRequestedDomainEvent(
    Guid CustomerId,
    string Name,
    string Email,
    string ResetUrl) : DomainEventBase;
