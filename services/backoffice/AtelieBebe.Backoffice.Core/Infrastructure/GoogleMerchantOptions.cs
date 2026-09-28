namespace AtelieBebe.Backoffice.Core.Infrastructure;

/// <summary>
/// Which products go to Google Merchant Center (Shopping). Empty (the default) means every active,
/// publicly listed product, so a new product reaches Google on its own; listing slugs here narrows
/// /api/google-merchant-feed.xml down to just those, in this order (in production,
/// <c>GoogleMerchant__ProductSlugs__N</c> env vars override the list).
/// </summary>
public sealed class GoogleMerchantOptions
{
    public const string SectionName = "GoogleMerchant";

    public List<string> ProductSlugs { get; set; } = [];
}
