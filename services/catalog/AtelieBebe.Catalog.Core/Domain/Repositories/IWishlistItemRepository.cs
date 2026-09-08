using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Domain.Repositories;

public interface IWishlistItemRepository
{
    Task<IReadOnlyList<WishlistItem>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<WishlistItem>> ListByProductAsync(Guid productId, CancellationToken ct = default);
    Task<WishlistItem?> GetAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    void Add(WishlistItem item);
    void Remove(WishlistItem item);
}
