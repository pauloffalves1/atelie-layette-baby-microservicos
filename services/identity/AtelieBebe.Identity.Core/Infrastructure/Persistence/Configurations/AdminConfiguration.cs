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

        builder.Property(a => a.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(a => a.Email).IsUnique();
    }
}
