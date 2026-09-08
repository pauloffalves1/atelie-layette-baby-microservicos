using System.Net.Http.Json;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Infrastructure.ExternalServices;

public sealed class OrdersServiceClient : IOrdersServiceClient
{
    private readonly HttpClient _httpClient;

    public OrdersServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<OrdersDashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<OrdersDashboardStatsDto>("/internal/orders/dashboard-stats", ct);
        return result ?? new OrdersDashboardStatsDto(0, 0, 0, 0, 0, [], [], [], []);
    }
}
