using System.Net;
using System.Text;
using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Infrastructure;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Catalog.Api.Endpoints;

/// <summary>
/// Server-rendered Open Graph/Twitter Card previews for link-unfurling bots (WhatsApp, Facebook,
/// Telegram, etc.) that fetch a URL's raw HTML and never execute JavaScript — the Angular SPA's own
/// client-side SeoService (Meta/Title, set after the page's data loads) is invisible to them, so a
/// shared product link would otherwise show a generic/blank preview. Real browsers never hit this
/// directly: Nginx only routes here when the request's User-Agent matches a known bot pattern (see
/// the VPS's nginx site config), everyone else gets the normal Angular app.
/// </summary>
public static class SeoEndpoints
{
    private const string SiteName = "Ateliê Layette Baby";
    private const string DefaultDescription = "Fraldas de ombro e boca bordadas com muito carinho para os primeiros dias do seu bebê.";

    public static void MapSeoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/seo").WithTags("SEO (bots)");

        group.MapGet("/product/{slug}", async (string slug, IProductService service, IOptions<AppUrlOptions> appUrls, CancellationToken ct) =>
        {
            var siteUrl = appUrls.Value.PublicUrl.TrimEnd('/');

            try
            {
                var product = await service.GetBySlugAsync(slug, null, ct);
                var description = string.IsNullOrWhiteSpace(product.Description)
                    ? $"{product.Name} — peça bordada do {SiteName}, feita sob medida com carinho."
                    : product.Description;
                var image = string.IsNullOrWhiteSpace(product.ImageUrl) ? $"{siteUrl}/images/hero-fraldas.jpg" : ToAbsolute(product.ImageUrl, siteUrl);
                var url = $"{siteUrl}/produto/{product.Slug}";

                return Results.Text(BuildHtml(
                    title: $"{product.Name} — {SiteName}",
                    description: description,
                    image: image,
                    url: url,
                    type: "product",
                    bodyHeading: product.Name,
                    bodyText: description), "text/html", Encoding.UTF8);
            }
            catch (AtelieBebe.SharedKernel.Exceptions.NotFoundException)
            {
                return Results.Text(BuildDefaultHtml(siteUrl), "text/html", Encoding.UTF8, statusCode: 404);
            }
        });

        group.MapGet("/default", (IOptions<AppUrlOptions> appUrls) =>
        {
            var siteUrl = appUrls.Value.PublicUrl.TrimEnd('/');
            return Results.Text(BuildDefaultHtml(siteUrl), "text/html", Encoding.UTF8);
        });
    }

    private static string BuildDefaultHtml(string siteUrl) => BuildHtml(
        title: SiteName,
        description: DefaultDescription,
        image: $"{siteUrl}/images/hero-fraldas.jpg",
        url: siteUrl,
        type: "website",
        bodyHeading: SiteName,
        bodyText: DefaultDescription);

    private static string ToAbsolute(string url, string siteUrl) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{siteUrl}{url}";

    private static string BuildHtml(string title, string description, string image, string url, string type, string bodyHeading, string bodyText)
    {
        string E(string s) => WebUtility.HtmlEncode(s);

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <title>{{E(title)}}</title>
              <meta name="description" content="{{E(description)}}">
              <link rel="canonical" href="{{E(url)}}">

              <meta property="og:title" content="{{E(title)}}">
              <meta property="og:description" content="{{E(description)}}">
              <meta property="og:type" content="{{E(type)}}">
              <meta property="og:url" content="{{E(url)}}">
              <meta property="og:image" content="{{E(image)}}">
              <meta property="og:site_name" content="{{E(SiteName)}}">

              <meta name="twitter:card" content="summary_large_image">
              <meta name="twitter:title" content="{{E(title)}}">
              <meta name="twitter:description" content="{{E(description)}}">
              <meta name="twitter:image" content="{{E(image)}}">
            </head>
            <body>
              <h1>{{E(bodyHeading)}}</h1>
              <p>{{E(bodyText)}}</p>
              <a href="{{E(url)}}">{{E(url)}}</a>
            </body>
            </html>
            """;
    }
}
