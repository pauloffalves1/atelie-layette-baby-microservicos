using System.Text;
using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.MerchantFeed;
using AtelieBebe.Backoffice.Core.Infrastructure;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class GoogleMerchantFeedEndpoints
{
    /// <summary>
    /// The product feed Google Merchant Center fetches on a schedule — generated per request, like
    /// /api/sitemap.xml, so it always carries the current price and photos. Only the products listed
    /// in <see cref="GoogleMerchantOptions.ProductSlugs"/> go in; the XML is built by
    /// <see cref="GoogleMerchantFeedBuilder"/>.
    /// </summary>
    public static void MapGoogleMerchantFeedEndpoints(this WebApplication app)
    {
        app.MapMethods("/api/google-merchant-feed.xml", [HttpMethods.Get, HttpMethods.Head], async (ICatalogServiceClient catalogServiceClient, IOptions<AppUrlOptions> appUrls, IOptions<GoogleMerchantOptions> merchant, CancellationToken ct) =>
        {
            var products = await catalogServiceClient.GetMerchantFeedProductsAsync(merchant.Value.ProductSlugs, ct);
            var xml = GoogleMerchantFeedBuilder.Build(appUrls.Value.PublicUrl, products);
            return Results.Text(xml, "application/xml", Encoding.UTF8);
        }).WithTags("Google Merchant");
    }
}
