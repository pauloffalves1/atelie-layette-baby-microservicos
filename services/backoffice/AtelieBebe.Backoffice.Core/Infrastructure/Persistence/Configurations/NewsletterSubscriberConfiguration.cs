using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Configurations;

public sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("NewsletterSubscribers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(s => s.Email).IsUnique();
    }
}
