using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.Orders.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Orders.Core.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _dbContext;

    public OrderRepository(OrdersDbContext dbContext) => _dbContext = dbContext;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order?> GetByShortIdAndEmailAsync(string shortId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = Email.Create(email);
        var orders = await _dbContext.Orders.Include(o => o.Items)
            .Where(o => o.CustomerEmail == normalizedEmail)
            .ToListAsync(ct);

        // Guid.ToString() isn't translatable to SQL by the SQLite provider, so the short-id
        // prefix match happens in memory — fine since a single customer's order count is small.
        return orders.FirstOrDefault(o => o.Id.ToString().StartsWith(shortId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalItems)> ListAsync(OrderStatus? status, PaymentStatus? paymentStatus, int page, int pageSize, CancellationToken ct = default)
    {
        var query = FilteredQuery(status, paymentStatus);

        var totalItems = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalItems);
    }

    /// <summary>Unpaginated — used only for CSV export, never for a UI listing.</summary>
    public async Task<IReadOnlyList<Order>> ListAllAsync(OrderStatus? status, PaymentStatus? paymentStatus, CancellationToken ct = default) =>
        await FilteredQuery(status, paymentStatus).ToListAsync(ct);

    private IQueryable<Order> FilteredQuery(OrderStatus? status, PaymentStatus? paymentStatus)
    {
        var query = _dbContext.Orders.Include(o => o.Items).AsQueryable();

        if (status is not null)
            query = query.Where(o => o.Status == status);

        if (paymentStatus is not null)
            query = query.Where(o => o.PaymentStatus == paymentStatus);

        return query.OrderByDescending(o => o.CreatedAt);
    }

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(ct);

    public Task<bool> CustomerHasPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken ct = default) =>
        _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .SelectMany(o => o.Items)
            .AnyAsync(i => i.ProductId == productId, ct);

    public void Add(Order order) => _dbContext.Orders.Add(order);
}
