using Microsoft.Extensions.Primitives;

namespace AtelieBebe.Catalog.Core.Application.Products;

/// <summary>
/// Shared invalidation signal for the public product listing cache (see ProductService.ListAsync/
/// ListFeaturedAsync/ListCategoriesAsync) — every write that could change what a customer sees
/// (create/update/delete/activate/promotion/images/allowed customers) calls Invalidate() so the next
/// read misses the cache immediately, instead of waiting out the TTL.
/// </summary>
public sealed class ProductCacheInvalidator
{
    private CancellationTokenSource _cts = new();

    public IChangeToken GetToken() => new CancellationChangeToken(_cts.Token);

    public void Invalidate()
    {
        var previous = _cts;
        _cts = new CancellationTokenSource();
        previous.Cancel();
        previous.Dispose();
    }
}
