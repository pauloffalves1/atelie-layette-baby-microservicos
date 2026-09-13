using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence;

public class BackofficeDbContext : DbContext, IOutboxDbContext
{
    public BackofficeDbContext(DbContextOptions<BackofficeDbContext> options) : base(options) { }

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // Stored DateTimes are UTC; read them back as UTC so the API serializes them with "Z" — see UtcDateTimeConventions.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseUtcDateTimes();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackofficeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
