using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Domain.Repositories;

namespace AtelieBebe.Catalog.Api.Endpoints;

/// <summary>Service-to-service only — never routed through the Gateway.</summary>
public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this WebApplication app)
    {
        // Notifications resolves who to email when a product comes back in stock — same
        // resolution the monolith did in-process at outbox-dispatch time, now over HTTP.
        app.MapGet("/internal/wishlist/by-product/{productId:guid}", async (Guid productId, IWishlistItemRepository wishlistItems, CancellationToken ct) =>
        {
            var items = await wishlistItems.ListByProductAsync(productId, ct);
            return Results.Ok(items.Select(i => i.CustomerId));
        });
        // Orders validates the current price before creating a store order (the unit price the
        // client sent is never trusted for a real catalog product — same rule the monolith enforced
        // in-process, now enforced across a service boundary instead).
        app.MapGet("/internal/products/{id:guid}", async (Guid id, IProductService service, CancellationToken ct) =>
        {
            var product = await service.GetByIdAsync(id, ct);
            // IsTest travels with the product so Orders can flag the order at creation (RF40) without
            // a second round trip or a copy of the rule.
            return Results.Ok(new { product.Id, product.Name, product.Slug, product.EffectivePrice, product.Active, product.IsTest });
        });

        // Backoffice dashboard's "TotalProducts" figure (API composition).
        app.MapGet("/internal/products/count", async (IProductService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(category: null, onlyActive: false, page: 1, pageSize: 1, ct: ct);
            return Results.Ok(new { count = result.TotalItems });
        });

        // Backoffice's sitemap generator — pages through everything so the catalog can grow past
        // one page without silently truncating the sitemap.
        app.MapGet("/internal/products/active-slugs", async (IProductService service, CancellationToken ct) =>
        {
            const int pageSize = 200;
            var slugs = new List<string>();
            var page = 1;
            while (true)
            {
                var result = await service.ListAsync(category: null, onlyActive: true, page: page, pageSize: pageSize, ct: ct);
                slugs.AddRange(result.Items.Select(p => p.Slug));
                if (slugs.Count >= result.TotalItems || result.Items.Count == 0) break;
                page++;
            }
            return Results.Ok(slugs);
        });

        // Backoffice's sitemap: slug plus what it needs for <lastmod>, category landing URLs and
        // <image:image> entries. Same "active and publicly listed" set as active-slugs above.
        app.MapGet("/internal/products/sitemap-entries", async (IProductService service, CancellationToken ct) =>
        {
            const int pageSize = 200;
            var entries = new List<object>();
            var page = 1;
            while (true)
            {
                var result = await service.ListAsync(category: null, onlyActive: true, page: page, pageSize: pageSize, ct: ct);
                entries.AddRange(result.Items.Select(p => new
                {
                    p.Slug,
                    p.Category,
                    p.UpdatedAt,
                    ImageUrls = (p.ImageUrl is null ? Array.Empty<string>() : [p.ImageUrl]).Concat(p.ImageUrls).ToList(),
                }));
                if (entries.Count >= result.TotalItems || result.Items.Count == 0) break;
                page++;
            }
            return Results.Ok(entries);
        });
    }
}
