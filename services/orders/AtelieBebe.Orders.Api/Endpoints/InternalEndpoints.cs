using AtelieBebe.Orders.Core.Application.Dashboard;
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
                .Where(o => o.Status != OrderStatus.Cancelado && !o.IsTest)
                .ToListAsync(ct);

            return Results.Ok(OrdersDashboardStatsCalculator.Calculate(orders.Select(DashboardOrderSnapshot.From), DateTime.UtcNow));
        });
    }
}
