namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Backoffice's read-only dependencies on Catalog — product count for the dashboard, active slugs for the sitemap.</summary>
public interface ICatalogServiceClient
{
    Task<int> GetProductCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetActiveProductSlugsAsync(CancellationToken ct = default);
}
