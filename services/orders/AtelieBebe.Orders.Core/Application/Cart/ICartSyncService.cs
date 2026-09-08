namespace AtelieBebe.Orders.Core.Application.Cart;

public interface ICartSyncService
{
    /// <summary>Upserts the customer's cart snapshot; an empty item list deletes it instead (nothing to remind about).</summary>
    Task SaveAsync(Guid customerId, CartSyncRequest request, CancellationToken ct = default);
}
