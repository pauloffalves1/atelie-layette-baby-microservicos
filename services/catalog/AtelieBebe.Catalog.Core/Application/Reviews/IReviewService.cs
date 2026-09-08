namespace AtelieBebe.Catalog.Core.Application.Reviews;

public interface IReviewService
{
    Task<IReadOnlyList<ProductReviewDto>> ListByProductAsync(Guid productId, CancellationToken ct = default);
    Task<ReviewEligibilityDto> GetEligibilityAsync(Guid productId, Guid customerId, CancellationToken ct = default);
    Task<ProductReviewDto> CreateAsync(Guid productId, Guid customerId, string customerName, CreateReviewRequest request, CancellationToken ct = default);
}
