using AtelieBebe.Backoffice.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdminName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Details).IsRequired().HasMaxLength(2000);

        builder.HasIndex(a => a.CreatedAt);
    }
}
