namespace AtelieBebe.SharedKernel.Common;

// Shared shape between Orders' internal dashboard-stats endpoint and Backoffice's dashboard
// (API composition: Backoffice merges this with a product count from Catalog and a customer
// count from Identity into the final DashboardDto the admin UI receives).

public sealed record OrdersByStatusDto(string Status, int Count);

public sealed record RecentOrderSummaryDto(Guid Id, string CustomerName, string Status, decimal Total, DateTime CreatedAt);

public sealed record TopProductDto(string ProductName, int QuantitySold, decimal Revenue);

public sealed record SalesByDayDto(DateTime Date, decimal Revenue, int OrderCount);

public sealed record OrdersDashboardStatsDto(
    int TotalOrders,
    int OpenOrders,
    decimal RevenueTotal,
    decimal RevenueThisMonth,
    decimal AverageOrderValue,
    IReadOnlyList<OrdersByStatusDto> OrdersByStatus,
    IReadOnlyList<RecentOrderSummaryDto> RecentOrders,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<SalesByDayDto> SalesLast30Days);
