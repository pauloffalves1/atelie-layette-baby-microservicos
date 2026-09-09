using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.PhotoUrl).HasMaxLength(500);
        // Existing reviews (created before moderation existed) are grandfathered in as approved —
        // new ones always pass an explicit false from ProductReview's constructor, overriding this.
        builder.Property(r => r.Approved).HasDefaultValue(true);

        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => r.Approved);
        // One review per customer per product — CreateAsync also checks this up front for a
        // friendlier error message, but the index is the actual guarantee against a race.
        builder.HasIndex(r => new { r.ProductId, r.CustomerId }).IsUnique();
    }
}
