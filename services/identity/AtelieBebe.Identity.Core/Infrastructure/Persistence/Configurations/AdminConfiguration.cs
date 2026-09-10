using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence.Configurations;

public sealed class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.PasswordHash).IsRequired();
        builder.Property(a => a.TwoFactorSecret).HasMaxLength(64);
        // No model-level HasDefaultValue here on purpose: EF Core treats AdminPermission.None (0)
        // as this property's CLR sentinel, so a model default would silently override every future
        // Admin.Create(..., AdminPermission.None) — a real case (a brand-new admin awaiting
        // permissions) — into full access instead. The one-time backfill for pre-existing rows is
        // done at the SQL level instead, in the AddAdminPermissions migration's AddColumn call.

        builder.Property(a => a.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(a => a.Email).IsUnique();
    }
}
