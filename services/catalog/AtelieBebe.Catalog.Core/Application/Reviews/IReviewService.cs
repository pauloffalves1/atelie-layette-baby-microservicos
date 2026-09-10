using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Catalog.Core.Application.Reviews;

public interface IReviewService
{
    Task<IReadOnlyList<ProductReviewDto>> ListByProductAsync(Guid productId, CancellationToken ct = default);
    Task<ReviewEligibilityDto> GetEligibilityAsync(Guid productId, Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<FeaturedReviewDto>> ListFeaturedAsync(int limit, CancellationToken ct = default);
    Task<ProductReviewDto> CreateAsync(Guid productId, Guid customerId, string customerName, CreateReviewRequest request, CancellationToken ct = default);

    Task<PagedResult<AdminProductReviewDto>> ListForAdminAsync(bool? approved, int page, int pageSize, CancellationToken ct = default);
    Task<AdminProductReviewDto> ApproveAsync(Guid id, CancellationToken ct = default);
    Task RejectAsync(Guid id, CancellationToken ct = default);
}
