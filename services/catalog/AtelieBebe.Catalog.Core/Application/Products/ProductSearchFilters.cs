namespace AtelieBebe.Catalog.Core.Application.Products;

/// <summary>Structured filters extracted from a free-text search query, to be run against the same product query as the regular catalog listing.</summary>
public sealed record ProductSearchFilters(
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Keywords,
    bool OnlyOnPromotion);
