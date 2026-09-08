namespace AtelieBebe.Notifications.Worker.ExternalServices;

/// <summary>Only used for the "back in stock" wishlist notification — everything else arrives fully-enriched in the event payload.</summary>
public interface ICatalogServiceClient
{
    Task<IReadOnlyList<Guid>> GetWishlistingCustomerIdsAsync(Guid productId, CancellationToken ct = default);
}
