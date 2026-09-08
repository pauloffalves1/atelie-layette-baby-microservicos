using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Configurations;

public sealed class ProductCustomerAccessEntryConfiguration : IEntityTypeConfiguration<ProductCustomerAccessEntry>
{
    public void Configure(EntityTypeBuilder<ProductCustomerAccessEntry> builder)
    {
        builder.ToTable("ProductCustomerAccess");
        builder.HasKey("ProductId", "CustomerId");
        builder.Property(a => a.CustomerId).IsRequired();
    }
}
