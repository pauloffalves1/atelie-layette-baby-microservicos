namespace AtelieBebe.Backoffice.Core.Infrastructure;

/// <summary>
/// Which products go to Google Merchant Center (Shopping). Only the slugs listed here appear in
/// /api/google-merchant-feed.xml, in this order — adding a product to Google is adding its slug
/// (in production, <c>GoogleMerchant__ProductSlugs__N</c> env vars override the list).
/// </summary>
public sealed class GoogleMerchantOptions
{
    public const string SectionName = "GoogleMerchant";

    public List<string> ProductSlugs { get; set; } = [];
}
