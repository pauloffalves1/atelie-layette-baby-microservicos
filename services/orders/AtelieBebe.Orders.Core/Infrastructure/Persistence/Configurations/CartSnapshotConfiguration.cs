using AtelieBebe.Orders.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence.Configurations;

public sealed class CartSnapshotConfiguration : IEntityTypeConfiguration<CartSnapshot>
{
    public void Configure(EntityTypeBuilder<CartSnapshot> builder)
    {
        builder.ToTable("CartSnapshots");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ItemsJson).IsRequired();

        builder.HasIndex(c => c.CustomerId).IsUnique();
    }
}
