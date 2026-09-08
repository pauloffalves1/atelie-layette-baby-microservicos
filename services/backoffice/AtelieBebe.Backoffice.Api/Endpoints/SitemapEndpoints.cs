using System.Text;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Infrastructure;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class SitemapEndpoints
{
    private static readonly string[] StaticPaths =
    [
        "/",
        "/loja",
        "/sobre",
        "/galeria",
        "/contato",
    ];

    /// <summary>
    /// Generated at request time (not a static file) so it always reflects the current catalog —
    /// robots.txt points crawlers at /api/sitemap.xml, reusing the /api/* proxy rule Nginx already
    /// has in production instead of needing a dedicated route at the SPA's own root. Product slugs
    /// now come from Catalog's internal API instead of a local query (Backoffice doesn't own products).
    /// </summary>
    public static void MapSitemapEndpoints(this WebApplication app)
    {
        app.MapGet("/api/sitemap.xml", async (ICatalogServiceClient catalogServiceClient, IOptions<AppUrlOptions> appUrls, CancellationToken ct) =>
        {
            var siteUrl = appUrls.Value.PublicUrl.TrimEnd('/');
            var slugs = await catalogServiceClient.GetActiveProductSlugsAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            foreach (var path in StaticPaths)
                AppendUrl(sb, $"{siteUrl}{path}", "weekly");

            foreach (var slug in slugs)
                AppendUrl(sb, $"{siteUrl}/produto/{slug}", "monthly");

            sb.AppendLine("</urlset>");

            return Results.Text(sb.ToString(), "application/xml");
        }).WithTags("Sitemap");
    }

    private static void AppendUrl(StringBuilder sb, string loc, string changeFreq)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(loc)}</loc>");
        sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
        sb.AppendLine("  </url>");
    }
}
