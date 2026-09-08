using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Application.Wishlist;

public sealed class WishlistService : IWishlistService
{
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(ICatalogUnitOfWork unitOfWork, ILogger<WishlistService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WishlistItemDto>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListByCustomerAsync));
        try
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

            _logger.LogInformation("Saindo de {Method}", nameof(ListByCustomerAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListByCustomerAsync));
            throw;
        }
    }

    public async Task<WishlistStatusDto> GetStatusAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetStatusAsync));
        try
        {
            var item = await _unitOfWork.WishlistItems.GetAsync(customerId, productId, ct);

            _logger.LogInformation("Saindo de {Method}", nameof(GetStatusAsync));
            return new WishlistStatusDto(item is not null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetStatusAsync));
            throw;
        }
    }

    public async Task AddAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(AddAsync));
        try
        {
            var existing = await _unitOfWork.WishlistItems.GetAsync(customerId, productId, ct);
            if (existing is not null)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(AddAsync));
                return;
            }

            var product = await _unitOfWork.Products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException("Produto", productId);

            _unitOfWork.WishlistItems.Add(WishlistItem.Create(customerId, product.Id));
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(AddAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(AddAsync));
            throw;
        }
    }

    public async Task RemoveAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(RemoveAsync));
        try
        {
            var existing = await _unitOfWork.WishlistItems.GetAsync(customerId, productId, ct);
            if (existing is null)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(RemoveAsync));
                return;
            }

            _unitOfWork.WishlistItems.Remove(existing);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(RemoveAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RemoveAsync));
            throw;
        }
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Price.Amount, p.Category, p.ImageUrl,
        p.Active, p.Featured, p.IsExclusive, p.ImageUrls,
        p.DiscountPercentage, p.PromotionStartsAt, p.PromotionEndsAt, p.IsOnPromotion, p.EffectivePrice.Amount);
}
