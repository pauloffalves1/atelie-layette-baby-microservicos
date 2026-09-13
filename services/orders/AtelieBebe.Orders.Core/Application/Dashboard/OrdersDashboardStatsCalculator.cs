using AtelieBebe.Orders.Core.Domain.Entities;
using AtelieBebe.Orders.Core.Domain.Enums;
using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Orders.Core.Application.Dashboard;

/// <summary>The slice of an order the dashboard needs — lets the aggregation be unit-tested with
/// arbitrary creation times, which the <see cref="Order"/> entity itself never allows.</summary>
public sealed record DashboardOrderSnapshot(
    Guid Id,
    string CustomerName,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal Total,
    DateTime CreatedAtUtc,
    IReadOnlyList<DashboardItemSnapshot> Items)
{
    public static DashboardOrderSnapshot From(Order order) => new(
        order.Id,
        order.CustomerName,
        order.Status,
        order.PaymentStatus,
        order.Total.Amount,
        order.CreatedAt,
        order.Items.Select(i => new DashboardItemSnapshot(i.ProductName, i.Quantity, i.Subtotal.Amount, i.ModerationFlag is not null)).ToList());
}

public sealed record DashboardItemSnapshot(string ProductName, int Quantity, decimal Subtotal, bool IsFlagged);

/// <summary>
/// Aggregates the admin dashboard figures. Day and month boundaries use the atelier's local time
/// (Brasília): grouping by UTC date put every order placed after 21:00 on the next day's bar and
/// flipped "this month" three hours early.
/// </summary>
public static class OrdersDashboardStatsCalculator
{
    public const int SalesWindowDays = 30;
    private const int RecentOrdersCount = 8;
    private const int TopProductsCount = 5;
    private const int FlaggedOrdersListed = 5;

    /// <summary>Workflow order, so the dashboard lists statuses the way an order moves through them.</summary>
    private static readonly OrderStatus[] StatusFlow =
        [OrderStatus.Recebido, OrderStatus.EmProducao, OrderStatus.Pronto, OrderStatus.Enviado, OrderStatus.Entregue];

    /// <summary>Brazil has had no daylight saving since 2019, so a fixed UTC-3 is a safe fallback when
    /// the container image has no tz database.</summary>
    public static TimeZoneInfo BrasiliaTimeZone { get; } = ResolveBrasiliaTimeZone();

    public static OrdersDashboardStatsDto Calculate(IEnumerable<DashboardOrderSnapshot> allOrders, DateTime utcNow, TimeZoneInfo? zone = null)
    {
        zone ??= BrasiliaTimeZone;
        var orders = allOrders.Where(o => o.Status != OrderStatus.Cancelado).ToList();

        DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);

        var nowLocal = ToLocal(utcNow);
        var startOfMonth = new DateTime(nowLocal.Year, nowLocal.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        // Month-to-date vs the same stretch of last month — comparing a half-finished month against a
        // whole one would always look like a drop.
        var elapsedThisMonth = nowLocal - startOfMonth;
        var endOfComparablePeriod = startOfLastMonth + elapsedThisMonth;
        if (endOfComparablePeriod > startOfMonth) endOfComparablePeriod = startOfMonth;

        var withLocalTime = orders.Select(o => (Order: o, Local: ToLocal(o.CreatedAtUtc))).ToList();
        var thisMonth = withLocalTime.Where(x => x.Local >= startOfMonth).Select(x => x.Order).ToList();

        var ordersByStatus = StatusFlow
            .Select(status => new OrdersByStatusDto(status.ToString(), orders.Count(o => o.Status == status)))
            .Where(s => s.Count > 0)
            .ToList();

        var recentOrders = orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(RecentOrdersCount)
            .Select(ToSummary)
            .ToList();

        var topProducts = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.Subtotal)))
            .OrderByDescending(p => p.QuantitySold)
            .Take(TopProductsCount)
            .ToList();

        // Every one of the last 30 local days, including days without sales — a chart built only from
        // days that had orders squeezes them together and hides how sparse the month really was.
        var today = nowLocal.Date;
        var salesByDay = withLocalTime
            .Where(x => x.Local.Date > today.AddDays(-SalesWindowDays))
            .GroupBy(x => x.Local.Date)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(x => x.Order.Total), Count: g.Count()));
        var salesLast30Days = Enumerable.Range(0, SalesWindowDays)
            .Select(offset => today.AddDays(offset - (SalesWindowDays - 1)))
            .Select(day => salesByDay.TryGetValue(day, out var s) ? new SalesByDayDto(day, s.Revenue, s.Count) : new SalesByDayDto(day, 0, 0))
            .ToList();

        var pendingPayment = orders.Where(o => o.PaymentStatus == PaymentStatus.Pendente).ToList();

        // Flagged embroidery only needs attention while the piece hasn't been made/delivered yet.
        var flagged = orders
            .Where(o => (o.Status is OrderStatus.Recebido or OrderStatus.EmProducao) && o.Items.Any(i => i.IsFlagged))
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToList();

        var revenueTotal = orders.Sum(o => o.Total);

        return new OrdersDashboardStatsDto(
            TotalOrders: orders.Count,
            OpenOrders: orders.Count(o => o.Status is OrderStatus.Recebido or OrderStatus.EmProducao or OrderStatus.Pronto or OrderStatus.Enviado),
            RevenueTotal: revenueTotal,
            RevenueThisMonth: thisMonth.Sum(o => o.Total),
            AverageOrderValue: orders.Count > 0 ? Math.Round(revenueTotal / orders.Count, 2) : 0,
            OrdersByStatus: ordersByStatus,
            RecentOrders: recentOrders,
            TopProducts: topProducts,
            SalesLast30Days: salesLast30Days,
            RevenueThisMonthPaid: thisMonth.Where(o => o.PaymentStatus == PaymentStatus.Pago).Sum(o => o.Total),
            RevenueSamePeriodLastMonth: withLocalTime
                .Where(x => x.Local >= startOfLastMonth && x.Local < endOfComparablePeriod)
                .Sum(x => x.Order.Total),
            PendingPaymentOrders: pendingPayment.Count,
            PendingPaymentAmount: pendingPayment.Sum(o => o.Total),
            FlaggedOrdersCount: flagged.Count,
            FlaggedOrders: flagged.Take(FlaggedOrdersListed).Select(ToSummary).ToList());
    }

    // EF hands back CreatedAt with Kind=Unspecified, which serializes without an offset and makes the
    // browser read a UTC clock time as local (3h ahead in Brasília). Marking it UTC emits the "Z".
    private static RecentOrderSummaryDto ToSummary(DashboardOrderSnapshot o) =>
        new(o.Id, o.CustomerName, o.Status.ToString(), o.Total, DateTime.SpecifyKind(o.CreatedAtUtc, DateTimeKind.Utc));

    private static TimeZoneInfo ResolveBrasiliaTimeZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        }
        return TimeZoneInfo.CreateCustomTimeZone("Brasilia-fixed", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }
}
