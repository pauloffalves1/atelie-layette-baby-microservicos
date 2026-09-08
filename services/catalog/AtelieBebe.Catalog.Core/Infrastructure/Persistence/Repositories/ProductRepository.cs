using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _dbContext;

    public ProductRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    /// <summary>Products with the customer-access grants eagerly loaded, so IsExclusive/AllowedCustomerIds/HasAccess read correctly in memory.</summary>
    private IQueryable<Product> ProductsWithAccess =>
        _dbContext.Products.Include("_allowedCustomerAccess").Include("_images");

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        ProductsWithAccess.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        await ProductsWithAccess.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<Product?> GetBySlugAsync(string slug, Guid? customerId = null, CancellationToken ct = default) =>
        ApplyVisibility(ProductsWithAccess.Where(p => p.Slug == slug), customerId).FirstOrDefaultAsync(ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _dbContext.Products.AnyAsync(p => p.Slug == slug, ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId = null, string? search = null, CancellationToken ct = default)
    {
        var query = ProductsWithAccess;

        if (onlyActive)
            query = query.Where(p => p.Active);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));

        // Admin listings (onlyActive: false) show every product regardless of exclusivity;
        // only the customer-facing catalog (onlyActive: true) is restricted by access grants.
        if (onlyActive)
            query = ApplyVisibility(query, customerId);

        query = query.OrderBy(p => p.Name);

        var totalItems = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<Product>> ListFeaturedAsync(Guid? customerId = null, CancellationToken ct = default) =>
        await ApplyVisibility(ProductsWithAccess.Where(p => p.Active && p.Featured), customerId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListCategoriesAsync(Guid? customerId = null, CancellationToken ct = default) =>
        await ApplyVisibility(_dbContext.Products.Where(p => p.Active), customerId)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

    public void Add(Product product) => _dbContext.Products.Add(product);

    public void Remove(Product product) => _dbContext.Products.Remove(product);

    /// <summary>
    /// Restricts a product query to products that are public, or exclusive products the given customer
    /// was granted access to. Translated to SQL as an EXISTS subquery against ProductCustomerAccess.
    /// </summary>
    private IQueryable<Product> ApplyVisibility(IQueryable<Product> query, Guid? customerId)
    {
        var access = _dbContext.Set<ProductCustomerAccessEntry>();

        return query.Where(p =>
            !access.Any(a => EF.Property<Guid>(a, "ProductId") == p.Id) ||
            (customerId != null && access.Any(a => EF.Property<Guid>(a, "ProductId") == p.Id && a.CustomerId == customerId)));
    }
}
