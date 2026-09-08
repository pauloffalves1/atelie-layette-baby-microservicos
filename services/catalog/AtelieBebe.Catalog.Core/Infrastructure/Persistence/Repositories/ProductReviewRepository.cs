using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;

public sealed class ProductReviewRepository : IProductReviewRepository
{
    private readonly CatalogDbContext _dbContext;

    public ProductReviewRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, CancellationToken ct = default) =>
        await _dbContext.ProductReviews
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid productId, Guid customerId, CancellationToken ct = default) =>
        _dbContext.ProductReviews.AnyAsync(r => r.ProductId == productId && r.CustomerId == customerId, ct);

    public void Add(ProductReview review) => _dbContext.ProductReviews.Add(review);
}
