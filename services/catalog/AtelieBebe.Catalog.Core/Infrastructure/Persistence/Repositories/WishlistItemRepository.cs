using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;

public sealed class WishlistItemRepository : IWishlistItemRepository
{
    private readonly CatalogDbContext _dbContext;

    public WishlistItemRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<WishlistItem>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await _dbContext.WishlistItems
            .Where(w => w.CustomerId == customerId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WishlistItem>> ListByProductAsync(Guid productId, CancellationToken ct = default) =>
        await _dbContext.WishlistItems
            .Where(w => w.ProductId == productId)
            .ToListAsync(ct);

    public Task<WishlistItem?> GetAsync(Guid customerId, Guid productId, CancellationToken ct = default) =>
        _dbContext.WishlistItems.FirstOrDefaultAsync(w => w.CustomerId == customerId && w.ProductId == productId, ct);

    public void Add(WishlistItem item) => _dbContext.WishlistItems.Add(item);

    public void Remove(WishlistItem item) => _dbContext.WishlistItems.Remove(item);
}
