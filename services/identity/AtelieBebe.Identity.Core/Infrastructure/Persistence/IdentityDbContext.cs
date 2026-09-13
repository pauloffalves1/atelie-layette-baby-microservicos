using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence;

public class IdentityDbContext : DbContext, IOutboxDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // Stored DateTimes are UTC; read them back as UTC so the API serializes them with "Z" — see UtcDateTimeConventions.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseUtcDateTimes();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
