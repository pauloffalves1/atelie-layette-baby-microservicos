using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Catalog.Core.Application.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IOrdersServiceClient _ordersServiceClient;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(ICatalogUnitOfWork unitOfWork, IOrdersServiceClient ordersServiceClient, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _ordersServiceClient = ordersServiceClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductReviewDto>> ListByProductAsync(Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListByProductAsync));
        try
        {
            var reviews = await _unitOfWork.ProductReviews.ListByProductAsync(productId, ct);
            var result = reviews.Select(ToDto).ToList();

            _logger.LogInformation("Saindo de {Method}", nameof(ListByProductAsync));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListByProductAsync));
            throw;
        }
    }

    public async Task<ReviewEligibilityDto> GetEligibilityAsync(Guid productId, Guid customerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(GetEligibilityAsync));
        try
        {
            var hasPurchased = await _ordersServiceClient.CustomerHasPurchasedProductAsync(customerId, productId, ct);
            var alreadyReviewed = await _unitOfWork.ProductReviews.ExistsAsync(productId, customerId, ct);

            _logger.LogInformation("Saindo de {Method}", nameof(GetEligibilityAsync));
            return new ReviewEligibilityDto(hasPurchased, alreadyReviewed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetEligibilityAsync));
            throw;
        }
    }

    // customerName comes from the caller's own JWT claims (the endpoint reads it off HttpContext.User)
    // instead of Catalog calling Identity to look it up — the authenticated customer already carries
    // their own name in the token, so there's no real cross-service need here.
    public async Task<ProductReviewDto> CreateAsync(Guid productId, Guid customerId, string customerName, CreateReviewRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(CreateAsync));
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId, ct)
                ?? throw new NotFoundException("Produto", productId);

            var hasPurchased = await _ordersServiceClient.CustomerHasPurchasedProductAsync(customerId, productId, ct);
            if (!hasPurchased)
                throw new ConflictException("Você precisa ter comprado este produto para avaliá-lo.");

            var alreadyReviewed = await _unitOfWork.ProductReviews.ExistsAsync(productId, customerId, ct);
            if (alreadyReviewed)
                throw new ConflictException("Você já avaliou este produto.");

            var review = ProductReview.Create(product.Id, customerId, customerName, request.Rating, request.Comment, request.PhotoUrl);
            _unitOfWork.ProductReviews.Add(review);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(CreateAsync));
            return ToDto(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateAsync));
            throw;
        }
    }

    private static ProductReviewDto ToDto(ProductReview r) =>
        new(r.Id, r.ProductId, r.CustomerName, r.Rating, r.Comment, r.PhotoUrl, r.CreatedAt);
}
