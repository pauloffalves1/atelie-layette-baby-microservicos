using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Domain.Repositories;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence;

public sealed class CatalogUnitOfWork : ICatalogUnitOfWork
{
    private readonly CatalogDbContext _dbContext;

    public CatalogUnitOfWork(
        CatalogDbContext dbContext,
        IProductRepository products,
        IProductReviewRepository productReviews,
        IWishlistItemRepository wishlistItems,
        IGalleryImageRepository galleryImages,
        ISiteImageRepository siteImages)
    {
        _dbContext = dbContext;
        Products = products;
        ProductReviews = productReviews;
        WishlistItems = wishlistItems;
        GalleryImages = galleryImages;
        SiteImages = siteImages;
    }

    public IProductRepository Products { get; }
    public IProductReviewRepository ProductReviews { get; }
    public IWishlistItemRepository WishlistItems { get; }
    public IGalleryImageRepository GalleryImages { get; }
    public ISiteImageRepository SiteImages { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _dbContext.SaveChangesAsync(ct);
}
