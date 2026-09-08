using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Catalog.Core.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly ICatalogUnitOfWork _unitOfWork;

    public ProductService(ICatalogUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<PagedResult<ProductDto>> ListAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId = null, string? search = null, CancellationToken ct = default)
    {
        var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);
        var (products, totalItems) = await _unitOfWork.Products.ListAsync(category, onlyActive, normalizedPage, normalizedPageSize, customerId, search, ct);
        return new PagedResult<ProductDto>(products.Select(ToDto).ToList(), normalizedPage, normalizedPageSize, totalItems);
    }

    public async Task<IReadOnlyList<ProductDto>> ListFeaturedAsync(Guid? customerId = null, CancellationToken ct = default)
    {
        var products = await _unitOfWork.Products.ListFeaturedAsync(customerId, ct);
        return products.Select(ToDto).ToList();
    }

    public Task<IReadOnlyList<string>> ListCategoriesAsync(Guid? customerId = null, CancellationToken ct = default) =>
        _unitOfWork.Products.ListCategoriesAsync(customerId, ct);

    public async Task<ProductDto> GetBySlugAsync(string slug, Guid? customerId = null, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetBySlugAsync(slug, customerId, ct)
            ?? throw new NotFoundException("Produto", slug);
        return ToDto(product);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);
        return ToDto(product);
    }

    public async Task<AdminProductDto> GetForAdminAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);
        return ToAdminDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var slug = SlugHelper.Slugify(request.Name);
        if (await _unitOfWork.Products.SlugExistsAsync(slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var product = Product.Create(
            request.Name,
            slug,
            request.Description,
            Money.FromReais(request.Price),
            request.Category,
            request.ImageUrl,
            request.Featured);

        _unitOfWork.Products.Add(product);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);

        product.UpdateDetails(
            request.Name,
            request.Description,
            Money.FromReais(request.Price),
            request.Category,
            request.ImageUrl,
            request.Featured);

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);

        product.SetActive(active);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<AdminProductDto> SetAllowedCustomersAsync(Guid id, SetAllowedCustomersRequest request, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);

        product.SetAllowedCustomers(request.CustomerIds);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToAdminDto(product);
    }

    public async Task<AdminProductDto> SetImagesAsync(Guid id, SetProductImagesRequest request, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);

        product.SetImages(request.ImageUrls);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToAdminDto(product);
    }

    public async Task<AdminProductDto> SetPromotionAsync(Guid id, SetPromotionRequest request, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Produto", id);

        product.SetPromotion(request.DiscountPercentage, request.StartsAt, request.EndsAt);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToAdminDto(product);
    }

    public async Task<IReadOnlyList<AdminProductDto>> ApplyPromotionToManyAsync(BulkApplyPromotionRequest request, CancellationToken ct = default)
    {
        var products = await _unitOfWork.Products.ListByIdsAsync(request.ProductIds, ct);

        foreach (var product in products)
            product.SetPromotion(request.DiscountPercentage, request.StartsAt, request.EndsAt);

        await _unitOfWork.SaveChangesAsync(ct);
        return products.Select(ToAdminDto).ToList();
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price.Amount, p.Category, p.ImageUrl, p.Active, p.Featured, p.IsExclusive, p.ImageUrls,
        p.DiscountPercentage, p.PromotionStartsAt, p.PromotionEndsAt, p.IsOnPromotion, p.EffectivePrice.Amount);

    private static AdminProductDto ToAdminDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price.Amount, p.Category, p.ImageUrl, p.Active, p.Featured, p.IsExclusive, p.AllowedCustomerIds, p.ImageUrls,
        p.DiscountPercentage, p.PromotionStartsAt, p.PromotionEndsAt, p.IsOnPromotion, p.EffectivePrice.Amount);
}
