using System.Globalization;
using System.Security;
using System.Text;

namespace AtelieBebe.Backoffice.Core.Application.MerchantFeed;

/// <summary>A product as the Google Merchant Center feed needs it (from Catalog's internal API).</summary>
public sealed record MerchantFeedProduct(
    string Slug,
    string Name,
    string? Description,
    string Category,
    decimal Price,
    decimal EffectivePrice,
    bool IsOnPromotion,
    DateTime? PromotionStartsAt,
    DateTime? PromotionEndsAt,
    IReadOnlyList<string> ImageUrls);

/// <summary>
/// Builds /api/google-merchant-feed.xml — the RSS 2.0 product feed Merchant Center fetches on a
/// schedule, so price, promotion and photos follow the catalog without re-uploading anything.
/// Every piece is made to order, so each item is always <c>in_stock</c> (the 7–14 day production
/// time belongs in Merchant Center's handling time, not here), handmade with no GTIN
/// (<c>identifier_exists</c> = no), and the ateliê itself is the brand. Shipping and the regional
/// free-shipping thresholds are configured in Merchant Center, not per item.
/// </summary>
public static class GoogleMerchantFeedBuilder
{
    public const string Brand = "Ateliê Layette Baby";

    // Merchant Center's own limits: title 150 chars, description 5000, up to 10 extra images.
    private const int MaxTitleLength = 150;
    private const int MaxDescriptionLength = 5000;
    private const int MaxAdditionalImages = 10;

    public static string Build(string siteUrl, IReadOnlyList<MerchantFeedProduct> products)
    {
        siteUrl = siteUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rss version=\"2.0\" xmlns:g=\"http://base.google.com/ns/1.0\">");
        sb.AppendLine("  <channel>");
        sb.AppendLine($"    <title>{Escape(Brand)}</title>");
        sb.AppendLine($"    <link>{Escape(siteUrl)}</link>");
        sb.AppendLine("    <description>Enxoval de bebê artesanal, feito sob encomenda.</description>");

        foreach (var product in products)
        {
            var images = product.ImageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => ToAbsolute(url, siteUrl))
                .Distinct()
                .ToList();
            // An item with no photo is rejected by Merchant Center anyway.
            if (images.Count == 0) continue;

            sb.AppendLine("    <item>");
            AppendField(sb, "id", product.Slug);
            AppendField(sb, "title", Truncate(product.Name.Trim(), MaxTitleLength));
            var description = string.IsNullOrWhiteSpace(product.Description) ? product.Name : product.Description.Trim();
            AppendField(sb, "description", Truncate(description, MaxDescriptionLength));
            AppendField(sb, "link", $"{siteUrl}/produto/{Uri.EscapeDataString(product.Slug)}");
            AppendField(sb, "image_link", images[0]);
            foreach (var image in images.Skip(1).Take(MaxAdditionalImages))
                AppendField(sb, "additional_image_link", image);
            AppendField(sb, "availability", "in_stock");
            AppendField(sb, "price", FormatPrice(product.Price));
            if (product.IsOnPromotion && product.EffectivePrice < product.Price)
            {
                AppendField(sb, "sale_price", FormatPrice(product.EffectivePrice));
                if (product.PromotionStartsAt is { } start && product.PromotionEndsAt is { } end)
                    AppendField(sb, "sale_price_effective_date", $"{FormatDate(start)}/{FormatDate(end)}");
            }
            AppendField(sb, "condition", "new");
            AppendField(sb, "brand", Brand);
            AppendField(sb, "identifier_exists", "no");
            if (!string.IsNullOrWhiteSpace(product.Category))
                AppendField(sb, "product_type", product.Category.Trim());
            sb.AppendLine("    </item>");
        }

        sb.AppendLine("  </channel>");
        sb.AppendLine("</rss>");
        return sb.ToString();
    }

    private static void AppendField(StringBuilder sb, string name, string value) =>
        sb.AppendLine($"      <g:{name}>{Escape(value)}</g:{name}>");

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private static string FormatPrice(decimal value) =>
        $"{value.ToString("0.00", CultureInfo.InvariantCulture)} BRL";

    private static string FormatDate(DateTime value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max].TrimEnd();

    private static string ToAbsolute(string url, string siteUrl) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{siteUrl}/{url.TrimStart('/')}";
}
