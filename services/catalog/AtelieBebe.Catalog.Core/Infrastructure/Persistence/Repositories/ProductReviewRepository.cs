using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;

public sealed class ProductReviewRepository : IProductReviewRepository
{
    private readonly CatalogDbContext _dbContext;

    public ProductReviewRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, bool onlyApproved, CancellationToken ct = default)
    {
        var query = _dbContext.ProductReviews.Where(r => r.ProductId == productId);
        if (onlyApproved)
            query = query.Where(r => r.Approved);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid productId, Guid customerId, CancellationToken ct = default) =>
        _dbContext.ProductReviews.AnyAsync(r => r.ProductId == productId && r.CustomerId == customerId, ct);

    public Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.ProductReviews.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IReadOnlyList<ProductReview> Items, int TotalItems)> ListForAdminAsync(bool? approved, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.ProductReviews.AsQueryable();
        if (approved.HasValue)
            query = query.Where(r => r.Approved == approved.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<ProductReview>> ListFeaturedAsync(int limit, CancellationToken ct = default) =>
        await _dbContext.ProductReviews
            .Where(r => r.Approved && r.Comment != null && r.Comment != "")
            .OrderByDescending(r => r.Rating)
            .ThenByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public void Add(ProductReview review) => _dbContext.ProductReviews.Add(review);
    public void Remove(ProductReview review) => _dbContext.ProductReviews.Remove(review);
}
