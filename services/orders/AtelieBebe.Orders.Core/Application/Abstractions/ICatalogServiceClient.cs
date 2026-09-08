namespace AtelieBebe.Orders.Core.Application.Abstractions;

public sealed record CatalogProductInfo(Guid Id, string Name, string Slug, decimal EffectivePrice, bool Active);

/// <summary>Orders' one real cross-service dependency: the authoritative current price/name of a catalog product — never trust the client-sent price.</summary>
public interface ICatalogServiceClient
{
    Task<CatalogProductInfo?> GetProductAsync(Guid productId, CancellationToken ct = default);
}
