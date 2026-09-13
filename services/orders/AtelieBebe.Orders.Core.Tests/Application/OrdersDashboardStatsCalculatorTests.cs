using AtelieBebe.Orders.Core.Application.Dashboard;
using AtelieBebe.Orders.Core.Domain.Enums;

namespace AtelieBebe.Orders.Core.Tests.Application;

/// <summary>Admin dashboard aggregation (RF36) — Brasília-local day/month boundaries, a gap-free
/// 30-day series, workflow-ordered statuses, paid/pending split and the month-over-month basis.</summary>
public class OrdersDashboardStatsCalculatorTests
{
    // Fixed UTC-3 so the tests don't depend on the machine's tz database.
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.CreateCustomTimeZone("test-brt", TimeSpan.FromHours(-3), "BRT", "BRT");

    private static DashboardOrderSnapshot Order(
        DateTime createdAtUtc,
        decimal total = 100m,
        OrderStatus status = OrderStatus.Recebido,
        PaymentStatus payment = PaymentStatus.Pago,
        bool flagged = false,
        string product = "Fralda de boca") =>
        new(Guid.NewGuid(), "Maria", status, payment, total, createdAtUtc,
            [new DashboardItemSnapshot(product, 1, total, flagged)]);

    [Fact]
    public void Calculate_OrderAfter21hBrasilia_CountsOnThatLocalDayNotTheNextUtcDay()
    {
        // 2026-09-13 23:30 UTC = 20:30 BRT on the 13th; 2026-09-14 01:30 UTC = 22:30 BRT, still the 13th.
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        var orders = new[]
        {
            Order(new DateTime(2026, 9, 13, 23, 30, 0, DateTimeKind.Utc), total: 50m),
            Order(new DateTime(2026, 9, 14, 1, 30, 0, DateTimeKind.Utc), total: 70m),
        };

        var stats = OrdersDashboardStatsCalculator.Calculate(orders, now, Brasilia);

        var sept13 = Assert.Single(stats.SalesLast30Days, d => d.Date == new DateTime(2026, 9, 13));
        Assert.Equal(120m, sept13.Revenue);
        Assert.Equal(2, sept13.OrderCount);
        Assert.Equal(0, stats.SalesLast30Days.Single(d => d.Date == new DateTime(2026, 9, 14)).OrderCount);
    }

    [Fact]
    public void Calculate_SalesSeries_HasEveryOneOfTheLast30LocalDaysEndingToday()
    {
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

        var stats = OrdersDashboardStatsCalculator.Calculate([Order(now.AddDays(-3))], now, Brasilia);

        Assert.Equal(30, stats.SalesLast30Days.Count);
        Assert.Equal(new DateTime(2026, 8, 16), stats.SalesLast30Days[0].Date);
        Assert.Equal(new DateTime(2026, 9, 14), stats.SalesLast30Days[^1].Date);
        Assert.Equal(29, stats.SalesLast30Days.Count(d => d.OrderCount == 0));
    }

    [Fact]
    public void Calculate_MonthStartsAtLocalMidnight()
    {
        // 2026-10-01 01:00 UTC is still 30/09 22:00 in Brasília — belongs to September, so on
        // 01/10 09:00 BRT it's neither "this month" nor in October's comparable slice of September.
        var order = Order(new DateTime(2026, 10, 1, 1, 0, 0, DateTimeKind.Utc));

        var octoberMorning = OrdersDashboardStatsCalculator.Calculate([order], new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc), Brasilia);
        var septemberNight = OrdersDashboardStatsCalculator.Calculate([order], new DateTime(2026, 10, 1, 2, 0, 0, DateTimeKind.Utc), Brasilia);

        Assert.Equal(0m, octoberMorning.RevenueThisMonth);
        Assert.Equal(100m, septemberNight.RevenueThisMonth);
    }

    [Fact]
    public void Calculate_RevenueThisMonth_KeepsEveryPaymentStatusAndBreaksOutPaidAndPending()
    {
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        var orders = new[]
        {
            Order(now.AddDays(-1), total: 100m, payment: PaymentStatus.Pago),
            Order(now.AddDays(-2), total: 40m, payment: PaymentStatus.Pendente),
            Order(now.AddDays(-2), total: 25m, payment: PaymentStatus.Pendente, status: OrderStatus.Cancelado),
        };

        var stats = OrdersDashboardStatsCalculator.Calculate(orders, now, Brasilia);

        Assert.Equal(140m, stats.RevenueThisMonth);
        Assert.Equal(100m, stats.RevenueThisMonthPaid);
        Assert.Equal(1, stats.PendingPaymentOrders); // the cancelled one isn't actionable
        Assert.Equal(40m, stats.PendingPaymentAmount);
    }

    [Fact]
    public void Calculate_ComparesMonthToDateWithTheSameStretchOfLastMonth()
    {
        // Now = 14/09 09:00 BRT. Last month's comparable window is 01/08 00:00 .. 14/08 09:00 BRT.
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        var orders = new[]
        {
            Order(new DateTime(2026, 8, 10, 15, 0, 0, DateTimeKind.Utc), total: 30m), // inside
            Order(new DateTime(2026, 8, 14, 11, 0, 0, DateTimeKind.Utc), total: 20m), // 08:00 BRT, inside
            Order(new DateTime(2026, 8, 14, 13, 0, 0, DateTimeKind.Utc), total: 999m), // 10:00 BRT, after the cut
            Order(new DateTime(2026, 8, 25, 15, 0, 0, DateTimeKind.Utc), total: 999m), // later in August
        };

        var stats = OrdersDashboardStatsCalculator.Calculate(orders, now, Brasilia);

        Assert.Equal(50m, stats.RevenueSamePeriodLastMonth);
    }

    [Fact]
    public void Calculate_ComparablePeriodNeverRunsPastTheEndOfAShorterLastMonth()
    {
        // 31/10 late evening: September only has 30 days, so the window is capped at 01/10 00:00.
        var now = new DateTime(2026, 11, 1, 2, 0, 0, DateTimeKind.Utc); // 31/10 23:00 BRT
        var orders = new[]
        {
            Order(new DateTime(2026, 9, 30, 20, 0, 0, DateTimeKind.Utc), total: 10m), // 30/09 17:00 BRT
            Order(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc), total: 500m), // October itself
        };

        var stats = OrdersDashboardStatsCalculator.Calculate(orders, now, Brasilia);

        Assert.Equal(10m, stats.RevenueSamePeriodLastMonth);
        Assert.Equal(500m, stats.RevenueThisMonth);
    }

    [Fact]
    public void Calculate_OrdersByStatus_FollowsTheWorkflowAndSkipsEmptyStatuses()
    {
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        var orders = new[]
        {
            Order(now, status: OrderStatus.Entregue),
            Order(now, status: OrderStatus.Recebido),
            Order(now, status: OrderStatus.Pronto),
            Order(now, status: OrderStatus.Cancelado),
        };

        var stats = OrdersDashboardStatsCalculator.Calculate(orders, now, Brasilia);

        Assert.Equal(["Recebido", "Pronto", "Entregue"], stats.OrdersByStatus.Select(s => s.Status));
    }

    [Fact]
    public void Calculate_FlaggedOrders_OnlyThoseNotYetProducedAndMarkedUtc()
    {
        var now = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        var orders = new[]
        {
            Order(now.AddHours(-1), status: OrderStatus.Recebido, flagged: true),
            Order(now.AddHours(-2), status: OrderStatus.EmProducao, flagged: true),
            Order(now.AddHours(-3), status: OrderStatus.Entregue, flagged: true),
            Order(now.AddHours(-4), status: OrderStatus.Recebido, flagged: false),
        };

        var stats = OrdersDashboardStatsCalculator.Calculate(orders, now, Brasilia);

        Assert.Equal(2, stats.FlaggedOrdersCount);
        Assert.NotNull(stats.FlaggedOrders);
        Assert.All(stats.FlaggedOrders!, o => Assert.Equal(DateTimeKind.Utc, o.CreatedAt.Kind));
    }
}
