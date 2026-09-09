namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Catalog's one real cross-service dependency: review eligibility needs to know whether a customer purchased the product — only Orders knows that.</summary>
public interface IOrdersServiceClient
{
    Task<bool> CustomerHasPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>Blocks product deletion — a product that appears in any order (any customer) can't be removed.</summary>
    Task<bool> HasAnyOrderForProductAsync(Guid productId, CancellationToken ct = default);
}
