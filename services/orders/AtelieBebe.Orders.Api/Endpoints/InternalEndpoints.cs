using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.Orders.Core.Domain.Repositories;
using AtelieBebe.Orders.Core.Infrastructure.Persistence;
using AtelieBebe.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Orders.Api.Endpoints;

/// <summary>Service-to-service only — never routed through the Gateway.</summary>
public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this WebApplication app)
    {
        // Catalog's review-eligibility check.
        app.MapGet("/internal/orders/has-purchased", async (Guid customerId, Guid productId, IOrderRepository orders, CancellationToken ct) =>
            Results.Ok(new { hasPurchased = await orders.CustomerHasPurchasedProductAsync(customerId, productId, ct) }));

        // Catalog's product-deletion guard — a product that appears in any order can't be deleted.
        app.MapGet("/internal/orders/has-any-for-product/{productId:guid}", async (Guid productId, IOrderRepository orders, CancellationToken ct) =>
            Results.Ok(new { hasOrders = await orders.HasAnyOrderForProductAsync(productId, ct) }));

        // Identity's account-deletion decision (remove vs. anonymize).
        app.MapGet("/internal/orders/has-any-for-customer/{customerId:guid}", async (Guid customerId, IOrderRepository orders, CancellationToken ct) =>
        {
            var list = await orders.ListByCustomerAsync(customerId, ct);
            return Results.Ok(new { hasOrders = list.Count > 0 });
        });

        // Backoffice dashboard (API composition) — everything except product/customer counts,
        // which live in Catalog/Identity. Same aggregation the monolith's DashboardService did,
        // just scoped to what this service alone owns.
        app.MapGet("/internal/orders/dashboard-stats", async (OrdersDbContext dbContext, CancellationToken ct) =>
        {
            var orders = await dbContext.Orders
                .Include(o => o.Items)
                .Where(o => o.Status != OrderStatus.Cancelado)
                .ToListAsync(ct);

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var ordersByStatus = orders
                .GroupBy(o => o.Status)
                .Select(g => new OrdersByStatusDto(g.Key.ToString(), g.Count()))
                .OrderBy(s => s.Status)
                .ToList();

            var recentOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(8)
                .Select(o => new RecentOrderSummaryDto(o.Id, o.CustomerName, o.Status.ToString(), o.Total.Amount, o.CreatedAt))
                .ToList();

            var topProducts = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new TopProductDto(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.Subtotal.Amount)))
                .OrderByDescending(p => p.QuantitySold)
                .Take(5)
                .ToList();

            var salesWindowStart = DateTime.UtcNow.Date.AddDays(-29);
            var salesLast30Days = orders
                .Where(o => o.CreatedAt >= salesWindowStart)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new SalesByDayDto(g.Key, g.Sum(o => o.Total.Amount), g.Count()))
                .OrderBy(s => s.Date)
                .ToList();

            return Results.Ok(new OrdersDashboardStatsDto(
                TotalOrders: orders.Count,
                OpenOrders: orders.Count(o => o.Status is OrderStatus.Recebido or OrderStatus.EmProducao or OrderStatus.Pronto or OrderStatus.Enviado),
                RevenueTotal: orders.Sum(o => o.Total.Amount),
                RevenueThisMonth: orders.Where(o => o.CreatedAt >= startOfMonth).Sum(o => o.Total.Amount),
                AverageOrderValue: orders.Count > 0 ? Math.Round(orders.Sum(o => o.Total.Amount) / orders.Count, 2) : 0,
                OrdersByStatus: ordersByStatus,
                RecentOrders: recentOrders,
                TopProducts: topProducts,
                SalesLast30Days: salesLast30Days));
        });
    }
}
