using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Configurations;

public sealed class SiteImageConfiguration : IEntityTypeConfiguration<SiteImage>
{
    public void Configure(EntityTypeBuilder<SiteImage> builder)
    {
        builder.ToTable("SiteImages");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Url).IsRequired().HasMaxLength(500);
        builder.Property(s => s.SortOrder).IsRequired();

        // No longer unique: a key like "home-hero" can hold several images rendered as a
        // carousel. Single-image keys (e.g. "about") just happen to only ever have one row.
        builder.HasIndex(s => s.Key);
    }
}
