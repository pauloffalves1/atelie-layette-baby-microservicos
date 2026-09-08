using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Identity.Core.Domain.Entities;

/// <summary>
/// A single-use, time-limited token confirming a customer actually controls the e-mail address
/// they registered with — same hash-only-persisted shape as <see cref="PasswordResetToken"/>, kept
/// as its own type rather than reused generically since the two represent different intents.
/// </summary>
public sealed class EmailVerificationToken : Entity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsValid => UsedAt is null && ExpiresAt > DateTime.UtcNow;

    private EmailVerificationToken() { } // EF Core

    private EmailVerificationToken(Guid id, Guid customerId, string tokenHash, DateTime expiresAt) : base(id)
    {
        CustomerId = customerId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public static EmailVerificationToken Create(Guid customerId, string tokenHash, TimeSpan validFor)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O hash do token é obrigatório.");

        return new EmailVerificationToken(Guid.NewGuid(), customerId, tokenHash, DateTime.UtcNow.Add(validFor));
    }

    public void MarkUsed()
    {
        if (!IsValid)
            throw new DomainException("Este link de verificação não é mais válido.");

        UsedAt = DateTime.UtcNow;
    }
}
