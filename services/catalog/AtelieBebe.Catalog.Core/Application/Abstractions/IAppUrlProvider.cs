namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>
/// The Angular app's own public origin (e.g. https://layettebaby.com.br) — needed whenever
/// Application-layer code has to build an absolute URL to hand to the customer (wishlist reminder
/// product link), since it can't reference Infrastructure's AppUrlOptions directly.
/// </summary>
public interface IAppUrlProvider
{
    string PublicUrl { get; }
}
