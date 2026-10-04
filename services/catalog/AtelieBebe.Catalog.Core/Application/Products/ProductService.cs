using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Application.Products;

public sealed class ProductService : IProductService
{
    // Public storefront browsing (category/search/featured/categories, no customerId) is the
    // overwhelming majority of Catalog traffic and hits SQL Server on every request; a short TTL
    // cache (backed by ProductCacheInvalidator so writes bust it immediately, not just after the
    // TTL) trades a few seconds of staleness for a lot less load under normal browsing spikes.
    // Personalized reads (customerId set, for exclusive-product access) always skip the cache.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IOrdersServiceClient _ordersServiceClient;
    private readonly ISemanticSearchTranslator _semanticSearchTranslator;
    private readonly IMemoryCache _cache;
    private readonly ProductCacheInvalidator _cacheInvalidator;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ICatalogUnitOfWork unitOfWork,
        IOrdersServiceClient ordersServiceClient,
        ISemanticSearchTranslator semanticSearchTranslator,
        IMemoryCache cache,
        ProductCacheInvalidator cacheInvalidator,
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _ordersServiceClient = ordersServiceClient;
        _semanticSearchTranslator = semanticSearchTranslator;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> ListAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId = null, bool isTestCustomer = false, string? search = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);

            if (customerId is not null)
                return await ListUncachedAsync(category, onlyActive, normalizedPage, normalizedPageSize, customerId, isTestCustomer, search, ct);

            var cacheKey = $"products:list:{category}:{onlyActive}:{normalizedPage}:{normalizedPageSize}:{search}";
            var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetOptions(new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }
                    .AddExpirationToken(_cacheInvalidator.GetToken()));
                return await ListUncachedAsync(category, onlyActive, normalizedPage, normalizedPageSize, customerId, isTestCustomer, search, ct);
            });

            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }

    private async Task<PagedResult<ProductDto>> ListUncachedAsync(string? category, bool onlyActive, int page, int pageSize, Guid? customerId, bool isTestCustomer, string? search, CancellationToken ct)
    {
        var (products, totalItems) = await _unitOfWork.Products.ListAsync(category, onlyActive, page, pageSize, customerId, isTestCustomer, search, ct);
        return new PagedResult<ProductDto>(products.Select(ToDto).ToList(), page, pageSize, totalItems);
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(string naturalLanguageQuery, int page, int pageSize, Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SearchAsync));
        try
        {
            var (normalizedPage, normalizedPageSize) = Pagination.Normalize(page, pageSize);

            var categories = await ListCategoriesAsync(customerId, isTestCustomer, ct);
            var filters = await _semanticSearchTranslator.TranslateAsync(naturalLanguageQuery, categories, ct);
            var (products, totalItems) = await _unitOfWork.Products.SearchAsync(filters, normalizedPage, normalizedPageSize, customerId, isTestCustomer, ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SearchAsync));
            return new PagedResult<ProductDto>(products.Select(ToDto).ToList(), normalizedPage, normalizedPageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SearchAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<ProductDto>> ListFeaturedAsync(Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListFeaturedAsync));
        try
        {
            if (customerId is not null)
                return (await _unitOfWork.Products.ListFeaturedAsync(customerId, isTestCustomer, ct)).Select(ToDto).ToList();

            var result = await _cache.GetOrCreateAsync("products:featured", async entry =>
            {
                entry.SetOptions(new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }
                    .AddExpirationToken(_cacheInvalidator.GetToken()));
                var products = await _unitOfWork.Products.ListFeaturedAsync(customerId, isTestCustomer, ct);
                return products.Select(ToDto).ToList();
            });

            _logger.LogInformation("Saindo de {Method}", nameof(ListFeaturedAsync));
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListFeaturedAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ListCategoriesAsync(Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListCategoriesAsync));
        try
        {
            if (customerId is not null)
                return await _unitOfWork.Products.ListCategoriesAsync(customerId, isTestCustomer, ct);

            var result = await _cache.GetOrCreateAsync("products:categories", async entry =>
            {
                entry.SetOptions(new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }
                    .AddExpirationToken(_cacheInvalidator.GetToken()));
                return await _unitOfWork.Products.ListCategoriesAsync(customerId, isTestCustomer, ct);
            });

            _logger.LogInformation("Saindo de {Method}", nameof(ListCategoriesAsync));
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListCategoriesAsync));
            throw;
        }
    }

    public async Task<ProductDto> GetBySlugAsync(string slug, Guid? customerId = null, bool isTestCustomer = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetBySlugAsync));
        try
        {
            var product = await _unitOfWork.Products.GetBySlugAsync(slug, customerId, isTestCustomer, ct)
                ?? throw new NotFoundException("Produto", slug);

            _logger.LogInformation("Saindo de {Method}", nameof(GetBySlugAsync));
            return ToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetBySlugAsync));
            throw;
        }
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetByIdAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            _logger.LogInformation("Saindo de {Method}", nameof(GetByIdAsync));
            return ToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<AdminProductDto> GetForAdminAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetForAdminAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            _logger.LogInformation("Saindo de {Method}", nameof(GetForAdminAsync));
            return ToAdminDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetForAdminAsync));
            throw;
        }
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(CreateAsync));
        try
        {
            var slug = SlugHelper.Slugify(request.Name);
            if (await _unitOfWork.Products.SlugExistsAsync(slug, ct: ct))
                slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

            var product = Product.Create(
                request.Name,
                slug,
                request.Description,
                Money.FromReais(request.Price),
                request.Category,
                request.ImageUrl,
                request.Featured,
                request.ProductionLeadTimeDays);

            _unitOfWork.Products.Add(product);
            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(CreateAsync));
            return ToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateAsync));
            throw;
        }
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(UpdateAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            product.UpdateDetails(
                request.Name,
                request.Description,
                Money.FromReais(request.Price),
                request.Category,
                request.ImageUrl,
                request.Featured,
                request.ProductionLeadTimeDays);

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                var slug = SlugHelper.Slugify(request.Slug);
                if (slug.Length == 0)
                    throw new DomainException("O link do produto precisa ter ao menos uma letra ou número.");
                if (slug != product.Slug && await _unitOfWork.Products.SlugExistsAsync(slug, product.Id, ct))
                    throw new ConflictException($"O link '{slug}' já é (ou já foi) usado por outro produto.");

                product.ChangeSlug(slug);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(UpdateAsync));
            return ToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(UpdateAsync));
            throw;
        }
    }

    public async Task<ProductDto> SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetActiveAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            product.SetActive(active);
            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(SetActiveAsync));
            return ToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetActiveAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(DeleteAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            if (await _ordersServiceClient.HasAnyOrderForProductAsync(id, ct))
                throw new ConflictException($"'{product.Name}' já faz parte de encomendas e não pode ser excluído — inative o produto em vez disso.");

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(DeleteAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(DeleteAsync));
            throw;
        }
    }

    public async Task<AdminProductDto> SetAllowedCustomersAsync(Guid id, SetAllowedCustomersRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetAllowedCustomersAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            product.SetAllowedCustomers(request.CustomerIds);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(SetAllowedCustomersAsync));
            return ToAdminDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetAllowedCustomersAsync));
            throw;
        }
    }

    public async Task<AdminProductDto> SetTestAsync(Guid id, SetTestProductRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetTestAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            product.SetTest(request.IsTest);
            await _unitOfWork.SaveChangesAsync(ct);
            // The storefront caches product listings; a product that just became (or stopped being) a
            // test product has to leave/re-enter them right away, not on the next cache expiry.
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(SetTestAsync));
            return ToAdminDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetTestAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<AdminProductDto>> ListTestAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListTestAsync));
        try
        {
            var products = await _unitOfWork.Products.ListTestAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(ListTestAsync));
            return products.Select(ToAdminDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListTestAsync));
            throw;
        }
    }

    public async Task<AdminProductDto> SetImagesAsync(Guid id, SetProductImagesRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetImagesAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            product.SetImages(request.ImageUrls);
            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(SetImagesAsync));
            return ToAdminDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetImagesAsync));
            throw;
        }
    }

    public async Task<AdminProductDto> SetPromotionAsync(Guid id, SetPromotionRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(SetPromotionAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Produto", id);

            product.SetPromotion(request.DiscountPercentage, request.StartsAt, request.EndsAt);
            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();

            _logger.LogInformation("Saindo de {Method}", nameof(SetPromotionAsync));
            return ToAdminDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetPromotionAsync));
            throw;
        }
    }

    public async Task<IReadOnlyList<AdminProductDto>> ApplyPromotionToManyAsync(BulkApplyPromotionRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ApplyPromotionToManyAsync));
        try
        {
            var products = await _unitOfWork.Products.ListByIdsAsync(request.ProductIds, ct);

            foreach (var product in products)
                product.SetPromotion(request.DiscountPercentage, request.StartsAt, request.EndsAt);

            await _unitOfWork.SaveChangesAsync(ct);
            _cacheInvalidator.Invalidate();
            var result = products.Select(ToAdminDto).ToList();

            _logger.LogInformation("Saindo de {Method}", nameof(ApplyPromotionToManyAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ApplyPromotionToManyAsync));
            throw;
        }
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price.Amount, p.Category, p.ImageUrl, p.Active, p.Featured, p.IsExclusive, p.ImageUrls,
        p.DiscountPercentage, p.PromotionStartsAt, p.PromotionEndsAt, p.IsOnPromotion, p.EffectivePrice.Amount, p.ProductionLeadTimeDays, p.UpdatedAt, p.IsTest);

    private static AdminProductDto ToAdminDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price.Amount, p.Category, p.ImageUrl, p.Active, p.Featured, p.IsExclusive, p.AllowedCustomerIds, p.ImageUrls,
        p.DiscountPercentage, p.PromotionStartsAt, p.PromotionEndsAt, p.IsOnPromotion, p.EffectivePrice.Amount, p.ProductionLeadTimeDays, p.IsTest);
}
