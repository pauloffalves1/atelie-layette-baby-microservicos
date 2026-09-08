using System.Net.Http.Json;
using AtelieBebe.Identity.Core.Application.Abstractions;

namespace AtelieBebe.Identity.Core.Infrastructure.ExternalServices;

/// <summary>HTTP client for Orders' internal API — base address ("Services:Orders" config) points at the container/K8s service name, never the public Gateway.</summary>
public sealed class OrdersServiceClient : IOrdersServiceClient
{
    private readonly HttpClient _httpClient;

    public OrdersServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<bool> CustomerHasOrdersAsync(Guid customerId, CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<HasOrdersResponse>(
            $"/internal/orders/has-any-for-customer/{customerId}", ct);
        return result?.HasOrders ?? false;
    }

    private sealed record HasOrdersResponse(bool HasOrders);
}
