using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, Guid? customerId = null, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId = null, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListFeaturedAsync(Guid? customerId = null, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListCategoriesAsync(Guid? customerId = null, CancellationToken ct = default);
    void Add(Product product);
    void Remove(Product product);
}
