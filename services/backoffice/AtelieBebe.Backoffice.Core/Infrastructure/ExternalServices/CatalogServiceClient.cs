using System.Net.Http.Json;
using AtelieBebe.Backoffice.Core.Application.Abstractions;

namespace AtelieBebe.Backoffice.Core.Infrastructure.ExternalServices;

public sealed class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<int> GetProductCountAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<CountResponse>("/internal/products/count", ct);
        return result?.Count ?? 0;
    }

    public async Task<IReadOnlyList<string>> GetActiveProductSlugsAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<string>>("/internal/products/active-slugs", ct);
        return result ?? [];
    }

    private sealed record CountResponse(int Count);
}
