using AtelieBebe.Backoffice.Core.Domain.Entities;

namespace AtelieBebe.Backoffice.Core.Domain.Repositories;

public interface INewsletterSubscriberRepository
{
    Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<NewsletterSubscriber>> ListAsync(CancellationToken ct = default);
    void Add(NewsletterSubscriber subscriber);
}
