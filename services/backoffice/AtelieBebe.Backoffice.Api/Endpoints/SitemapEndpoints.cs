using System.Text;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.Sitemap;
using AtelieBebe.Backoffice.Core.Infrastructure;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class SitemapEndpoints
{
    /// <summary>
    /// Generated at request time (not a static file) so it always reflects the current catalog —
    /// robots.txt points crawlers at /api/sitemap.xml, reusing the /api/* proxy rule Nginx already
    /// has in production instead of needing a dedicated route at the SPA's own root. Products come
    /// from Catalog's internal API (Backoffice doesn't own them); the XML itself is built by
    /// <see cref="SitemapXmlBuilder"/>. It is also the URL list the crawler prerender job walks.
    /// </summary>
    public static void MapSitemapEndpoints(this WebApplication app)
    {
        app.MapGet("/api/sitemap.xml", async (ICatalogServiceClient catalogServiceClient, IOptions<AppUrlOptions> appUrls, CancellationToken ct) =>
        {
            var products = await catalogServiceClient.GetSitemapProductsAsync(ct);
            var xml = SitemapXmlBuilder.Build(appUrls.Value.PublicUrl, products);
            return Results.Text(xml, "application/xml", Encoding.UTF8);
        }).WithTags("Sitemap");
    }
}
