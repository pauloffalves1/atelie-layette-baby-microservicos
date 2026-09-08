using System.Net;
using System.Net.Http.Json;
using AtelieBebe.Orders.Core.Application.Abstractions;

namespace AtelieBebe.Orders.Core.Infrastructure.ExternalServices;

public sealed class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/internal/products/{productId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProductInfo>(cancellationToken: ct);
    }
}
