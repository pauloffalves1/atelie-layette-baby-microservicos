namespace AtelieBebe.Catalog.Core.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    bool Active,
    bool Featured,
    bool IsExclusive,
    IReadOnlyList<string> ImageUrls,
    decimal? DiscountPercentage,
    DateTime? PromotionStartsAt,
    DateTime? PromotionEndsAt,
    bool IsOnPromotion,
    decimal EffectivePrice,
    int? ProductionLeadTimeDays,
    DateTime UpdatedAt,
    bool IsTest = false);

public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    bool Active,
    bool Featured,
    bool IsExclusive,
    IReadOnlyCollection<Guid> AllowedCustomerIds,
    IReadOnlyList<string> ImageUrls,
    decimal? DiscountPercentage,
    DateTime? PromotionStartsAt,
    DateTime? PromotionEndsAt,
    bool IsOnPromotion,
    decimal EffectivePrice,
    int? ProductionLeadTimeDays,
    bool IsTest = false);

public sealed record SetPromotionRequest(decimal? DiscountPercentage, DateTime? StartsAt, DateTime? EndsAt);

/// <summary>Flips the test-product marker (RF40) — a product only the customers granted access can see, whose orders stay out of the admin's lists and figures.</summary>
public sealed record SetTestProductRequest(bool IsTest);

public sealed record BulkApplyPromotionRequest(IReadOnlyCollection<Guid> ProductIds, decimal? DiscountPercentage, DateTime? StartsAt, DateTime? EndsAt);

public sealed record SetAllowedCustomersRequest(IReadOnlyCollection<Guid> CustomerIds);

public sealed record SetProductImagesRequest(IReadOnlyList<string> ImageUrls);

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    bool Featured,
    int? ProductionLeadTimeDays = null);

public sealed record GenerateProductDescriptionRequest(string Name, string Category);

public sealed record GenerateProductDescriptionResponse(string Description);

/// <param name="Slug">New link for the product (/produto/{slug}). Null or the current one leaves it unchanged.</param>
public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    bool Featured,
    int? ProductionLeadTimeDays = null,
    string? Slug = null);
