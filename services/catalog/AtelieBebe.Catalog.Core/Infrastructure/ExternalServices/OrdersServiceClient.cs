using System.Net.Http.Json;
using AtelieBebe.Catalog.Core.Application.Abstractions;

namespace AtelieBebe.Catalog.Core.Infrastructure.ExternalServices;

public sealed class OrdersServiceClient : IOrdersServiceClient
{
    private readonly HttpClient _httpClient;

    public OrdersServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<bool> CustomerHasPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<HasPurchasedResponse>(
            $"/internal/orders/has-purchased?customerId={customerId}&productId={productId}", ct);
        return result?.HasPurchased ?? false;
    }

    private sealed record HasPurchasedResponse(bool HasPurchased);
}
