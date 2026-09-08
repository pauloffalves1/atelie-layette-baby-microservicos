using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Identity.Core.Domain.Entities;

/// <summary>
/// A single-use, time-limited credential that lets a customer set a new password without knowing
/// the old one. Only the SHA-256 hash of the raw token is ever persisted — the raw value lives only
/// in the reset e-mail link and the request that redeems it — so a database leak alone can't be used
/// to reset anyone's password.
/// </summary>
public sealed class PasswordResetToken : Entity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsValid => UsedAt is null && ExpiresAt > DateTime.UtcNow;

    private PasswordResetToken() { } // EF Core

    private PasswordResetToken(Guid id, Guid customerId, string tokenHash, DateTime expiresAt) : base(id)
    {
        CustomerId = customerId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public static PasswordResetToken Create(Guid customerId, string tokenHash, TimeSpan validFor)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("O hash do token é obrigatório.");

        return new PasswordResetToken(Guid.NewGuid(), customerId, tokenHash, DateTime.UtcNow.Add(validFor));
    }

    public void MarkUsed()
    {
        if (!IsValid)
            throw new DomainException("Este link de redefinição de senha não é mais válido.");

        UsedAt = DateTime.UtcNow;
    }
}
