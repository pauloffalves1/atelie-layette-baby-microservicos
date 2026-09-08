using AtelieBebe.Backoffice.Core.Application.Abstractions;

namespace AtelieBebe.Backoffice.Core.Application.Dashboard;

/// <summary>
/// API composition: the monolith's DashboardService queried one shared database directly; here the
/// same figures live in three different services' own databases, so this fans out to all three in
/// parallel and merges the results into the same DashboardDto shape the admin UI already expects.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IOrdersServiceClient _ordersServiceClient;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IIdentityServiceClient _identityServiceClient;

    public DashboardService(IOrdersServiceClient ordersServiceClient, ICatalogServiceClient catalogServiceClient, IIdentityServiceClient identityServiceClient)
    {
        _ordersServiceClient = ordersServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _identityServiceClient = identityServiceClient;
    }

    public async Task<DashboardDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var ordersStatsTask = _ordersServiceClient.GetDashboardStatsAsync(ct);
        var productCountTask = _catalogServiceClient.GetProductCountAsync(ct);
        var customerCountTask = _identityServiceClient.GetCustomerCountAsync(ct);

        await Task.WhenAll(ordersStatsTask, productCountTask, customerCountTask);

        var stats = ordersStatsTask.Result;

        return new DashboardDto(
            TotalOrders: stats.TotalOrders,
            OpenOrders: stats.OpenOrders,
            RevenueTotal: stats.RevenueTotal,
            RevenueThisMonth: stats.RevenueThisMonth,
            AverageOrderValue: stats.AverageOrderValue,
            TotalProducts: productCountTask.Result,
            TotalCustomers: customerCountTask.Result,
            OrdersByStatus: stats.OrdersByStatus,
            RecentOrders: stats.RecentOrders,
            TopProducts: stats.TopProducts,
            SalesLast30Days: stats.SalesLast30Days);
    }
}
