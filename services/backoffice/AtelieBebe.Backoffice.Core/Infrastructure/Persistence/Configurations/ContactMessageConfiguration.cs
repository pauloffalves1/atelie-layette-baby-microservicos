using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Configurations;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Phone).IsRequired().HasMaxLength(30).HasDefaultValue("");
        builder.Property(m => m.Message).IsRequired().HasMaxLength(4000);

        builder.Property(m => m.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();
    }
}
