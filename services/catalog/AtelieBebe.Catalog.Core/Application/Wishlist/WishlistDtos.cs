using AtelieBebe.Catalog.Core.Application.Products;

namespace AtelieBebe.Catalog.Core.Application.Wishlist;

public sealed record WishlistItemDto(Guid Id, ProductDto Product, DateTime CreatedAt);

public sealed record WishlistStatusDto(bool IsFavorited);
