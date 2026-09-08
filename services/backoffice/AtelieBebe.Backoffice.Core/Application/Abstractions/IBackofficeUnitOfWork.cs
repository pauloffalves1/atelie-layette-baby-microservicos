using AtelieBebe.Backoffice.Core.Domain.Repositories;

namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Backoffice service's own unit of work — contact messages, newsletter, audit log.</summary>
public interface IBackofficeUnitOfWork
{
    IContactMessageRepository ContactMessages { get; }
    INewsletterSubscriberRepository NewsletterSubscribers { get; }
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
