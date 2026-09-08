using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.Backoffice.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Repositories;

public sealed class NewsletterSubscriberRepository : INewsletterSubscriberRepository
{
    private readonly BackofficeDbContext _dbContext;

    public NewsletterSubscriberRepository(BackofficeDbContext dbContext) => _dbContext = dbContext;

    public Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = Email.Create(email);
        return _dbContext.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == normalized, ct);
    }

    public async Task<IReadOnlyList<NewsletterSubscriber>> ListAsync(CancellationToken ct = default) =>
        await _dbContext.NewsletterSubscribers.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public void Add(NewsletterSubscriber subscriber) => _dbContext.NewsletterSubscribers.Add(subscriber);
}
