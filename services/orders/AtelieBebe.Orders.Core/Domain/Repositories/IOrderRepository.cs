using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;

namespace AtelieBebe.Orders.Core.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Guest order tracking: matches the short id prefix shown to customers (Id.ToString()[..8]) against the customer's own e-mail, so knowing an order number alone isn't enough to look up someone else's order.</summary>
    Task<Order?> GetByShortIdAndEmailAsync(string shortId, string email, CancellationToken ct = default);
    Task<(IReadOnlyList<Order> Items, int TotalItems)> ListAsync(OrderStatus? status, PaymentStatus? paymentStatus, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Unpaginated — used only for CSV export, never for a UI listing.</summary>
    Task<IReadOnlyList<Order>> ListAllAsync(OrderStatus? status, PaymentStatus? paymentStatus, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Used to gate product reviews to customers who actually bought the product — any order status counts, not just delivered.</summary>
    Task<bool> CustomerHasPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>Used to block Catalog from deleting a product that appears in any order (any customer, any status) — preserves order history integrity.</summary>
    Task<bool> HasAnyOrderForProductAsync(Guid productId, CancellationToken ct = default);

    void Add(Order order);
}
