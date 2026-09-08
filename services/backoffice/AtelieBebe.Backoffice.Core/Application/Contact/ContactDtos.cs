namespace AtelieBebe.Backoffice.Core.Application.Contact;

public sealed record SubmitContactRequest(string Name, string Email, string Phone, string Message);

public sealed record ContactMessageDto(Guid Id, string Name, string Email, string Phone, string Message, DateTime CreatedAt);
