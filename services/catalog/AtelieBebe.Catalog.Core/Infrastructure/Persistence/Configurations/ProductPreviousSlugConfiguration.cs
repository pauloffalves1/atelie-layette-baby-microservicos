using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Configurations;

public sealed class ProductPreviousSlugConfiguration : IEntityTypeConfiguration<ProductPreviousSlug>
{
    public void Configure(EntityTypeBuilder<ProductPreviousSlug> builder)
    {
        builder.ToTable("ProductPreviousSlugs");
        builder.HasKey(s => s.Id);

        // Same length as Products.Slug. Unique: an old link leads to exactly one product.
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(s => s.Slug).IsUnique();
        builder.Property(s => s.ChangedAt).IsRequired();
    }
}
