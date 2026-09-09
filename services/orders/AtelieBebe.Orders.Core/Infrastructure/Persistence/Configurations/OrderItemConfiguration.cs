using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.OptionsJson);
        builder.Property(i => i.Quantity).IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasConversion(money => money.Amount, amount => Money.FromReais(amount))
            .HasColumnName("UnitPriceAmount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Ignore(i => i.Subtotal);
    }
}
