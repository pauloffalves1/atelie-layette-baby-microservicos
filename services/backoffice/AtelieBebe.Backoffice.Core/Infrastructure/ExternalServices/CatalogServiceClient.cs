using System.Net.Http.Json;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.MerchantFeed;
using AtelieBebe.Backoffice.Core.Application.Sitemap;

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

    public async Task<IReadOnlyList<SitemapProduct>> GetSitemapProductsAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SitemapProduct>>("/internal/products/sitemap-entries", ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<string>> GetActiveProductSlugsAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<string>>("/internal/products/active-slugs", ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<MerchantFeedProduct>> GetMerchantFeedProductsAsync(IReadOnlyList<string> slugs, CancellationToken ct = default)
    {
        if (slugs.Count == 0) return [];
        var query = string.Join("&", slugs.Select(s => $"slug={Uri.EscapeDataString(s)}"));
        var result = await _httpClient.GetFromJsonAsync<List<MerchantFeedProduct>>($"/internal/products/merchant-feed-entries?{query}", ct);
        return result ?? [];
    }

    private sealed record CountResponse(int Count);
}
