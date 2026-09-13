namespace AtelieBebe.SharedKernel.Common;

// Shared shape between Orders' internal dashboard-stats endpoint and Backoffice's dashboard
// (API composition: Backoffice merges this with a product count from Catalog and a customer
// count from Identity into the final DashboardDto the admin UI receives).

public sealed record OrdersByStatusDto(string Status, int Count);

public sealed record RecentOrderSummaryDto(Guid Id, string CustomerName, string Status, decimal Total, DateTime CreatedAt);

public sealed record TopProductDto(string ProductName, int QuantitySold, decimal Revenue);

/// <summary><paramref name="Date"/> is a calendar day in the atelier's local time (Brasília), not UTC.</summary>
public sealed record SalesByDayDto(DateTime Date, decimal Revenue, int OrderCount);

/// <summary>
/// Every figure excludes cancelled orders. "Revenue" counts every other order regardless of payment
/// status (orders settled over WhatsApp never get marked paid by the gateway), so the paid/pending
/// fields break it down rather than replace it. Fields after SalesLast30Days were added later and
/// default to empty, so a Backoffice build older/newer than Orders still deserializes the payload.
/// </summary>
public sealed record OrdersDashboardStatsDto(
    int TotalOrders,
    int OpenOrders,
    decimal RevenueTotal,
    decimal RevenueThisMonth,
    decimal AverageOrderValue,
    IReadOnlyList<OrdersByStatusDto> OrdersByStatus,
    IReadOnlyList<RecentOrderSummaryDto> RecentOrders,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<SalesByDayDto> SalesLast30Days,
    decimal RevenueThisMonthPaid = 0,
    decimal RevenueSamePeriodLastMonth = 0,
    int PendingPaymentOrders = 0,
    decimal PendingPaymentAmount = 0,
    int FlaggedOrdersCount = 0,
    IReadOnlyList<RecentOrderSummaryDto>? FlaggedOrders = null);
