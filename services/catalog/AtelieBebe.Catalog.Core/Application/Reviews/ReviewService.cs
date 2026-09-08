using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Application.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IOrdersServiceClient _ordersServiceClient;

    public ReviewService(ICatalogUnitOfWork unitOfWork, IOrdersServiceClient ordersServiceClient)
    {
        _unitOfWork = unitOfWork;
        _ordersServiceClient = ordersServiceClient;
    }

    public async Task<IReadOnlyList<ProductReviewDto>> ListByProductAsync(Guid productId, CancellationToken ct = default)
    {
        var reviews = await _unitOfWork.ProductReviews.ListByProductAsync(productId, ct);
        return reviews.Select(ToDto).ToList();
    }

    public async Task<ReviewEligibilityDto> GetEligibilityAsync(Guid productId, Guid customerId, CancellationToken ct = default)
    {
        var hasPurchased = await _ordersServiceClient.CustomerHasPurchasedProductAsync(customerId, productId, ct);
        var alreadyReviewed = await _unitOfWork.ProductReviews.ExistsAsync(productId, customerId, ct);
        return new ReviewEligibilityDto(hasPurchased, alreadyReviewed);
    }

    // customerName comes from the caller's own JWT claims (the endpoint reads it off HttpContext.User)
    // instead of Catalog calling Identity to look it up — the authenticated customer already carries
    // their own name in the token, so there's no real cross-service need here.
    public async Task<ProductReviewDto> CreateAsync(Guid productId, Guid customerId, string customerName, CreateReviewRequest request, CancellationToken ct = default)
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

        return ToDto(review);
    }

    private static ProductReviewDto ToDto(ProductReview r) =>
        new(r.Id, r.ProductId, r.CustomerName, r.Rating, r.Comment, r.PhotoUrl, r.CreatedAt);
}
