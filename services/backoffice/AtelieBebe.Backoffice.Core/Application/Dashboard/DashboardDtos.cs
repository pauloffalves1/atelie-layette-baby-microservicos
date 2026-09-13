using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Application.Dashboard;

// Per-status/top-product/sales-by-day shapes come from AtelieBebe.SharedKernel.Common — the exact
// same contract Orders' /internal/orders/dashboard-stats returns, reused here instead of redefined.

/// <summary>Fields after SalesLast30Days default to empty so the POST /summary body sent by an older
/// admin build (which doesn't have them yet) still binds.</summary>
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
    IReadOnlyList<SalesByDayDto> SalesLast30Days,
    decimal RevenueThisMonthPaid = 0,
    decimal RevenueSamePeriodLastMonth = 0,
    int PendingPaymentOrders = 0,
    decimal PendingPaymentAmount = 0,
    int FlaggedOrdersCount = 0,
    IReadOnlyList<RecentOrderSummaryDto>? FlaggedOrders = null);
