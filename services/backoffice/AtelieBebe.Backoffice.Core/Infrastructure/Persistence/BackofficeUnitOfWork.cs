using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Domain.Repositories;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence;

public sealed class BackofficeUnitOfWork : IBackofficeUnitOfWork
{
    private readonly BackofficeDbContext _dbContext;

    public BackofficeUnitOfWork(
        BackofficeDbContext dbContext,
        IContactMessageRepository contactMessages,
        INewsletterSubscriberRepository newsletterSubscribers,
        IAuditLogRepository auditLogs)
    {
        _dbContext = dbContext;
        ContactMessages = contactMessages;
        NewsletterSubscribers = newsletterSubscribers;
        AuditLogs = auditLogs;
    }

    public IContactMessageRepository ContactMessages { get; }
    public INewsletterSubscriberRepository NewsletterSubscribers { get; }
    public IAuditLogRepository AuditLogs { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _dbContext.SaveChangesAsync(ct);
}
