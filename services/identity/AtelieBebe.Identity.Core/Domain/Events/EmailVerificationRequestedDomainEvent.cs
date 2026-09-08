using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Identity.Core.Domain.Events;

public sealed record EmailVerificationRequestedDomainEvent(
    Guid CustomerId,
    string Name,
    string Email,
    string VerificationUrl) : DomainEventBase;
