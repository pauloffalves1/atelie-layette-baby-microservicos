using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default);
    /// <summary>Whether a product other than <paramref name="exceptProductId"/> uses <paramref name="slug"/>, as its current or a previous link.</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? exceptProductId = null, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId = null, bool isTestCustomer = false, string? search = null, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalItems)> SearchAsync(ProductSearchFilters filters, int page, int pageSize, Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListFeaturedAsync(Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListCategoriesAsync(Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default);

    /// <summary>Every test product (RF40), for the panel's test dashboard.</summary>
    Task<IReadOnlyList<Product>> ListTestAsync(CancellationToken ct = default);
    void Add(Product product);
    void Remove(Product product);
}
