using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CustomerPhone).HasMaxLength(30);
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.GiftMessage).HasMaxLength(500);
        builder.Property(o => o.RecipientName).HasMaxLength(200);
        builder.Property(o => o.DeliveryMethod).HasMaxLength(20).HasDefaultValue("Entrega").IsRequired();
        builder.Property(o => o.CustomDetailsJson).HasColumnType("TEXT");
        builder.Property(o => o.ShippingAddressJson).HasColumnType("TEXT");

        builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CustomerId);

        builder.Property(o => o.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(PaymentStatus.Pendente)
            .IsRequired();
        builder.Property(o => o.ExternalPaymentId).HasMaxLength(100);
        builder.Property(o => o.PixQrCodeText).HasMaxLength(600);
        builder.Property(o => o.TrackingCode).HasMaxLength(60);
        builder.Property(o => o.CouponCode).HasMaxLength(30);

        builder.Property(o => o.CouponDiscountAmount)
            .HasConversion(money => money.Amount, amount => Money.FromReais(amount))
            .HasColumnName("CouponDiscountAmount")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValueSql("0")
            .IsRequired();

        builder.Property(o => o.CustomerEmail)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("CustomerEmail")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(o => o.CustomerCpf)
            .HasConversion(cpf => cpf == null ? null : cpf.Value, value => value == null ? null : Cpf.Create(value))
            .HasColumnName("CustomerCpf")
            .HasMaxLength(11);

        builder.Property(o => o.ShippingCost)
            .HasConversion(money => money.Amount, amount => Money.FromReais(amount))
            .HasColumnName("ShippingCostAmount")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValueSql("0")
            .IsRequired();

        // Items are a child collection of the Order aggregate, only ever mutated through
        // Order's own methods (AddItem). EF is pointed at the private backing field so no
        // public setter is ever exposed to callers outside the aggregate.
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.ItemsTotal);
        builder.Ignore(o => o.Total);
    }
}
