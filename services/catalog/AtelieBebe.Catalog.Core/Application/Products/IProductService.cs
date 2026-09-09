using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Catalog.Core.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> ListAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId = null, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> ListFeaturedAsync(Guid? customerId = null, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListCategoriesAsync(Guid? customerId = null, CancellationToken ct = default);
    Task<ProductDto> GetBySlugAsync(string slug, Guid? customerId = null, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdminProductDto> GetForAdminAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> SetActiveAsync(Guid id, bool active, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<AdminProductDto> SetAllowedCustomersAsync(Guid id, SetAllowedCustomersRequest request, CancellationToken ct = default);
    Task<AdminProductDto> SetImagesAsync(Guid id, SetProductImagesRequest request, CancellationToken ct = default);
    Task<AdminProductDto> SetPromotionAsync(Guid id, SetPromotionRequest request, CancellationToken ct = default);

    /// <summary>Applies the same discount/window to every product in the list at once — bulk promotion setup.</summary>
    Task<IReadOnlyList<AdminProductDto>> ApplyPromotionToManyAsync(BulkApplyPromotionRequest request, CancellationToken ct = default);
}
