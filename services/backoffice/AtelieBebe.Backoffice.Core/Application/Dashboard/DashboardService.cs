using AtelieBebe.Backoffice.Core.Application.Abstractions;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IOrdersServiceClient ordersServiceClient, ICatalogServiceClient catalogServiceClient, IIdentityServiceClient identityServiceClient, ILogger<DashboardService> logger)
    {
        _ordersServiceClient = ordersServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _identityServiceClient = identityServiceClient;
        _logger = logger;
    }

    public async Task<DashboardDto> GetSummaryAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetSummaryAsync));
        try
        {
            var ordersStatsTask = _ordersServiceClient.GetDashboardStatsAsync(ct);
            var productCountTask = _catalogServiceClient.GetProductCountAsync(ct);
            var customerCountTask = _identityServiceClient.GetCustomerCountAsync(ct);

            await Task.WhenAll(ordersStatsTask, productCountTask, customerCountTask);

            var stats = ordersStatsTask.Result;

            var result = new DashboardDto(
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

            _logger.LogInformation("Saindo de {Method}", nameof(GetSummaryAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetSummaryAsync));
            throw;
        }
    }
}
