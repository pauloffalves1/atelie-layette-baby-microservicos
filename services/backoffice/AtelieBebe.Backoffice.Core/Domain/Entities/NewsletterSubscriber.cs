using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Backoffice.Core.Domain.Entities;

/// <summary>An e-mail captured for marketing campaigns (Mailchimp/Resend broadcast, etc.) — sending campaigns is not this system's job, just collecting/exporting the list.</summary>
public sealed class NewsletterSubscriber : Entity, IAggregateRoot
{
    public Email Email { get; private set; } = default!;
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NewsletterSubscriber() { } // EF Core

    private NewsletterSubscriber(Guid id, Email email) : base(id)
    {
        Email = email;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static NewsletterSubscriber Create(Email email)
    {
        if (email is null)
            throw new DomainException("O e-mail é obrigatório.");

        return new NewsletterSubscriber(Guid.NewGuid(), email);
    }

    public void Reactivate() => Active = true;

    public void Unsubscribe() => Active = false;
}
