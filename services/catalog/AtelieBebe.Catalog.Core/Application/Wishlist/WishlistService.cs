using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Application.Wishlist;

public sealed class WishlistService : IWishlistService
{
    private readonly ICatalogUnitOfWork _unitOfWork;

    public WishlistService(ICatalogUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<WishlistItemDto>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var items = await _unitOfWork.WishlistItems.ListByCustomerAsync(customerId, ct);

        var result = new List<WishlistItemDto>();
        foreach (var item in items)
        {
            // Defensive: a favorited product could in principle disappear (only ever happens via
            // DbInitializer's startup catalog cleanup, never a real admin action) — skip it rather
            // than fail the whole list.
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, ct);
            if (product is not null)
                result.Add(new WishlistItemDto(item.Id, ToDto(product), item.CreatedAt));
        }

        return result;
    }

    public async Task<WishlistStatusDto> GetStatusAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var item = await _unitOfWork.WishlistItems.GetAsync(customerId, productId, ct);
        return new WishlistStatusDto(item is not null);
    }

    public async Task AddAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.WishlistItems.GetAsync(customerId, productId, ct);
        if (existing is not null) return;

        var product = await _unitOfWork.Products.GetByIdAsync(productId, ct)
            ?? throw new NotFoundException("Produto", productId);

        _unitOfWork.WishlistItems.Add(WishlistItem.Create(customerId, product.Id));
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.WishlistItems.GetAsync(customerId, productId, ct);
        if (existing is null) return;

        _unitOfWork.WishlistItems.Remove(existing);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price.Amount, p.Category, p.ImageUrl,
        p.Active, p.Featured, p.IsExclusive, p.ImageUrls,
        p.DiscountPercentage, p.PromotionStartsAt, p.PromotionEndsAt, p.IsOnPromotion, p.EffectivePrice.Amount);
}
