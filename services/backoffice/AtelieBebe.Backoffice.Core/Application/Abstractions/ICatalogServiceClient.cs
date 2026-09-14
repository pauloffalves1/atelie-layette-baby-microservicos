using AtelieBebe.Backoffice.Core.Application.Sitemap;

namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Backoffice's read-only dependencies on Catalog — product count for the dashboard, active products for the sitemap.</summary>
public interface ICatalogServiceClient
{
    Task<int> GetProductCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SitemapProduct>> GetSitemapProductsAsync(CancellationToken ct = default);
}
