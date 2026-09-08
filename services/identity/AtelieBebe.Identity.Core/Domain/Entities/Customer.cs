using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Identity.Core.Domain.Events;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Identity.Core.Domain.Entities;

public sealed class Customer : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public Cpf? Cpf { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsAnonymized { get; private set; }
    public bool EmailVerified { get; private set; }

    public string? AddressStreet { get; private set; }
    public string? AddressNumber { get; private set; }
    public string? AddressComplement { get; private set; }
    public string? AddressNeighborhood { get; private set; }
    public string? AddressCity { get; private set; }
    public string? AddressState { get; private set; }
    public string? AddressZipCode { get; private set; }

    private Customer() { } // EF Core

    private Customer(Guid id, string name, Email email, Cpf cpf, string passwordHash, string? phone)
        : base(id)
    {
        Name = name;
        Email = email;
        Cpf = cpf;
        PasswordHash = passwordHash;
        Phone = phone;
        CreatedAt = DateTime.UtcNow;
    }

    public static Customer Register(
        string name, Email email, Cpf cpf, string passwordHash, string? phone,
        string? addressStreet = null, string? addressNumber = null, string? addressComplement = null,
        string? addressNeighborhood = null, string? addressCity = null, string? addressState = null, string? addressZipCode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome é obrigatório.");
        if (cpf is null)
            throw new DomainException("O CPF é obrigatório.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A senha é obrigatória.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("O telefone/WhatsApp é obrigatório.");

        var customer = new Customer(Guid.NewGuid(), name.Trim(), email, cpf, passwordHash, phone.Trim())
        {
            AddressStreet = addressStreet?.Trim(),
            AddressNumber = addressNumber?.Trim(),
            AddressComplement = addressComplement?.Trim(),
            AddressNeighborhood = addressNeighborhood?.Trim(),
            AddressCity = addressCity?.Trim(),
            AddressState = addressState?.Trim(),
            AddressZipCode = addressZipCode?.Trim(),
        };
        customer.AddDomainEvent(new CustomerRegisteredDomainEvent(customer.Id, customer.Name, customer.Email.Value, customer.Phone!));
        return customer;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("A senha é obrigatória.");

        PasswordHash = newPasswordHash;
    }

    /// <summary>Admin-only edit of a customer's own profile fields — uniqueness of email/CPF is checked by the caller before this is invoked, since that requires a repository lookup.</summary>
    public void UpdateDetails(
        string name, Email email, Cpf cpf, string? phone,
        string? addressStreet = null, string? addressNumber = null, string? addressComplement = null,
        string? addressNeighborhood = null, string? addressCity = null, string? addressState = null, string? addressZipCode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("O telefone/WhatsApp é obrigatório.");

        if (!Email.Equals(email)) EmailVerified = false;

        Name = name.Trim();
        Email = email;
        Cpf = cpf;
        Phone = phone.Trim();
        AddressStreet = addressStreet?.Trim();
        AddressNumber = addressNumber?.Trim();
        AddressComplement = addressComplement?.Trim();
        AddressNeighborhood = addressNeighborhood?.Trim();
        AddressCity = addressCity?.Trim();
        AddressState = addressState?.Trim();
        AddressZipCode = addressZipCode?.Trim();
    }

    /// <summary>Raises the event that carries a one-time reset link to the customer's e-mail — the link/token itself is generated and persisted by the application layer, this only records the intent.</summary>
    public void RequestPasswordReset(string resetUrl)
    {
        if (string.IsNullOrWhiteSpace(resetUrl))
            throw new DomainException("A URL de redefinição de senha é obrigatória.");

        AddDomainEvent(new PasswordResetRequestedDomainEvent(Id, Name, Email.Value, resetUrl));
    }

    /// <summary>Raises the event that carries a one-time confirmation link to the customer's e-mail — same generation/persistence split as <see cref="RequestPasswordReset"/>.</summary>
    public void RequestEmailVerification(string verificationUrl)
    {
        if (string.IsNullOrWhiteSpace(verificationUrl))
            throw new DomainException("A URL de verificação é obrigatória.");

        AddDomainEvent(new EmailVerificationRequestedDomainEvent(Id, Name, Email.Value, verificationUrl));
    }

    public void VerifyEmail() => EmailVerified = true;

    /// <summary>
    /// Scrubs personal data (LGPD account-deletion request) while keeping the row itself — orders
    /// already store their own snapshot of name/e-mail/phone/CPF at purchase time, so this never
    /// erases order history, only the ability to log in or be identified going forward.
    /// <paramref name="unusablePasswordHash"/> must be a real hash of an unguessable value (never a
    /// raw/malformed string) — Domain has no hashing abstraction, so the caller supplies it via
    /// <c>IPasswordHasher</c>.
    /// </summary>
    public void Anonymize(string unusablePasswordHash)
    {
        if (IsAnonymized) return;

        Name = "Cliente removido";
        Email = Email.Create($"cliente-removido-{Id}@removido.local");
        Cpf = null;
        Phone = null;
        AddressStreet = null;
        AddressNumber = null;
        AddressComplement = null;
        AddressNeighborhood = null;
        AddressCity = null;
        AddressState = null;
        AddressZipCode = null;
        PasswordHash = unusablePasswordHash;
        IsAnonymized = true;
        EmailVerified = false;
    }
}
