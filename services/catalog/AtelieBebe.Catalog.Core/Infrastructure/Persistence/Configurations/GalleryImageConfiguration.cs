using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Configurations;

public sealed class GalleryImageConfiguration : IEntityTypeConfiguration<GalleryImage>
{
    public void Configure(EntityTypeBuilder<GalleryImage> builder)
    {
        builder.ToTable("GalleryImages");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Url).IsRequired().HasMaxLength(500);
        builder.HasIndex(g => g.CreatedAt);
    }
}
