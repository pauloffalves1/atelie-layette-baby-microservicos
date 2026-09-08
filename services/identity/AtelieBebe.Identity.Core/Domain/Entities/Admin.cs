using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Identity.Core.Domain.Entities;

public sealed class Admin : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Admin() { } // EF Core

    private Admin(Guid id, string name, Email email, string passwordHash) : base(id)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public static Admin Create(string name, Email email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A senha é obrigatória.");

        return new Admin(Guid.NewGuid(), name.Trim(), email, passwordHash);
    }

    /// <summary>Persists the secret and turns 2FA on — only called after the caller already verified a code against this same secret.</summary>
    public void EnableTwoFactor(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new DomainException("Segredo de autenticação inválido.");

        TwoFactorSecret = secret;
        TwoFactorEnabled = true;
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
    }
}
