namespace AtelieBebe.Catalog.Core.Application.Wishlist;

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<WishlistStatusDto> GetStatusAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>Idempotent — favoriting an already-favorited product is a no-op, not an error.</summary>
    Task AddAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>Idempotent — un-favoriting a product that isn't favorited is a no-op, not an error.</summary>
    Task RemoveAsync(Guid customerId, Guid productId, CancellationToken ct = default);
}
