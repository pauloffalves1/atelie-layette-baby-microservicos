namespace AtelieBebe.Backoffice.Core.Application.Newsletter;

public interface INewsletterService
{
    /// <summary>Idempotent — subscribing twice, or resubscribing after unsubscribing, is a no-op success either way.</summary>
    Task SubscribeAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyList<NewsletterSubscriberDto>> ListAsync(CancellationToken ct = default);
}
