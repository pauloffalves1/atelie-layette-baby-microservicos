using AtelieBebe.Orders.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(30);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.DiscountPercentage).HasColumnType("decimal(5,2)").IsRequired();
    }
}
