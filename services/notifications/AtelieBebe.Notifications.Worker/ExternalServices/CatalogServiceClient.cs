using System.Net.Http.Json;

namespace AtelieBebe.Notifications.Worker.ExternalServices;

public sealed class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<Guid>> GetWishlistingCustomerIdsAsync(Guid productId, CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<Guid>>($"/internal/wishlist/by-product/{productId}", ct);
        return result ?? [];
    }
}
