using AtelieBebe.SharedKernel.Common;
using AtelieBebe.Backoffice.Core.Domain.Events;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Backoffice.Core.Domain.Entities;

public sealed class ContactMessage : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private ContactMessage() { } // EF Core

    private ContactMessage(Guid id, string name, Email email, string phone, string message) : base(id)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }

    public static ContactMessage Create(string name, Email email, string phone, string message)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("O telefone/WhatsApp é obrigatório.");
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("A mensagem é obrigatória.");

        var contactMessage = new ContactMessage(Guid.NewGuid(), name.Trim(), email, phone.Trim(), message.Trim());
        contactMessage.AddDomainEvent(new ContactMessageReceivedDomainEvent(contactMessage.Id, contactMessage.Name, contactMessage.Email.Value, contactMessage.Phone));
        return contactMessage;
    }
}
