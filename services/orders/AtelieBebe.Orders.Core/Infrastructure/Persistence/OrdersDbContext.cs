using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence;

public class OrdersDbContext : DbContext, IOutboxDbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CartSnapshot> CartSnapshots => Set<CartSnapshot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // Stored DateTimes are UTC; read them back as UTC so the API serializes them with "Z" — see UtcDateTimeConventions.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseUtcDateTimes();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
