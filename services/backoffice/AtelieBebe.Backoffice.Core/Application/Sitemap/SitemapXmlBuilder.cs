using System.Globalization;
using System.Security;
using System.Text;

namespace AtelieBebe.Backoffice.Core.Application.Sitemap;

/// <summary>An active, publicly listed product as the sitemap needs it (from Catalog's internal API).</summary>
public sealed record SitemapProduct(string Slug, string Category, DateTime UpdatedAt, IReadOnlyList<string> ImageUrls);

/// <summary>
/// Builds /api/sitemap.xml. Beyond the plain list of URLs it used to be, it now tells crawlers when
/// each product last changed (<c>lastmod</c> — the only freshness hint Google actually uses;
/// <c>changefreq</c> is ignored), lists every product photo for Google Images
/// (<c>image:image</c>), and includes one landing URL per category.
/// </summary>
public static class SitemapXmlBuilder
{
    public static readonly IReadOnlyList<string> StaticPaths =
    [
        "/",
        "/loja",
        "/sobre",
        "/dicas-para-o-casal",
        "/contato",
        "/politica-de-envio",
        "/politica-de-devolucao",
        "/perguntas-frequentes",
        "/termos-de-uso",
        "/politica-de-privacidade",
    ];

    public static string Build(string siteUrl, IReadOnlyList<SitemapProduct> products)
    {
        siteUrl = siteUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\">");

        var newest = products.Count > 0 ? products.Max(p => p.UpdatedAt) : (DateTime?)null;
        foreach (var path in StaticPaths)
        {
            // The home and the shop list products, so they change whenever a product does.
            var lastModified = path is "/" or "/loja" ? newest : null;
            AppendUrl(sb, $"{siteUrl}{path}", lastModified, []);
        }

        foreach (var category in products.GroupBy(p => p.Category.Trim()).Where(g => g.Key.Length > 0).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            AppendUrl(sb, $"{siteUrl}/loja?categoria={Uri.EscapeDataString(category.Key)}", category.Max(p => p.UpdatedAt), []);
        }

        foreach (var product in products)
        {
            var images = product.ImageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => ToAbsolute(url, siteUrl))
                .Distinct()
                .ToList();
            AppendUrl(sb, $"{siteUrl}/produto/{Uri.EscapeDataString(product.Slug)}", product.UpdatedAt, images);
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private static void AppendUrl(StringBuilder sb, string loc, DateTime? lastModified, IReadOnlyList<string> images)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{SecurityElement.Escape(loc)}</loc>");
        if (lastModified is { } date)
            sb.AppendLine($"    <lastmod>{date.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}</lastmod>");
        foreach (var image in images)
        {
            sb.AppendLine("    <image:image>");
            sb.AppendLine($"      <image:loc>{SecurityElement.Escape(image)}</image:loc>");
            sb.AppendLine("    </image:image>");
        }
        sb.AppendLine("  </url>");
    }

    private static string ToAbsolute(string url, string siteUrl) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{siteUrl}/{url.TrimStart('/')}";
}
