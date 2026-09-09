using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Domain.Repositories;

public interface IProductReviewRepository
{
    Task<IReadOnlyList<ProductReview>> ListByProductAsync(Guid productId, bool onlyApproved, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid productId, Guid customerId, CancellationToken ct = default);
    Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Admin moderation queue — newest first, optionally filtered by approval status.</summary>
    Task<(IReadOnlyList<ProductReview> Items, int TotalItems)> ListForAdminAsync(bool? approved, int page, int pageSize, CancellationToken ct = default);

    void Add(ProductReview review);
    void Remove(ProductReview review);
}
