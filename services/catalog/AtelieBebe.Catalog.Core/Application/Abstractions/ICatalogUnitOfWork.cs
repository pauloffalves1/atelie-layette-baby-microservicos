using AtelieBebe.Catalog.Core.Domain.Repositories;

namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Catalog service's own unit of work — products, reviews, wishlist, gallery, site images.</summary>
public interface ICatalogUnitOfWork
{
    IProductRepository Products { get; }
    IProductReviewRepository ProductReviews { get; }
    IWishlistItemRepository WishlistItems { get; }
    IGalleryImageRepository GalleryImages { get; }
    ISiteImageRepository SiteImages { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
