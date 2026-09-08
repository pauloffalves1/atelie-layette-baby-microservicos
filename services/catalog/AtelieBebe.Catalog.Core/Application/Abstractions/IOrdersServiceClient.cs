namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Catalog's one real cross-service dependency: review eligibility needs to know whether a customer purchased the product — only Orders knows that.</summary>
public interface IOrdersServiceClient
{
    Task<bool> CustomerHasPurchasedProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);
}
