namespace AtelieBebe.Backoffice.Core.Application.Newsletter;

public sealed record SubscribeNewsletterRequest(string Email);

public sealed record NewsletterSubscriberDto(Guid Id, string Email, DateTime CreatedAt);
