using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Application.Dashboard;

// Per-status/top-product/sales-by-day shapes come from AtelieBebe.SharedKernel.Common — the exact
// same contract Orders' /internal/orders/dashboard-stats returns, reused here instead of redefined.

public sealed record DashboardDto(
    int TotalOrders,
    int OpenOrders,
    decimal RevenueTotal,
    decimal RevenueThisMonth,
    decimal AverageOrderValue,
    int TotalProducts,
    int TotalCustomers,
    IReadOnlyList<OrdersByStatusDto> OrdersByStatus,
    IReadOnlyList<RecentOrderSummaryDto> RecentOrders,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<SalesByDayDto> SalesLast30Days);
