using System.Net;
using System.Text;
using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Infrastructure;
using AtelieBebe.Catalog.Core.Infrastructure.Storage;
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

    /// <summary>
    /// The photo a preview points at. Always JPEG at a known size: Facebook and Instagram do not
    /// decode WebP (the stored format), and without width/height they have to download and measure
    /// the file before they will draw a large card — so the first share of a link shows a grey box.
    /// </summary>
    private readonly record struct PreviewImage(string Url, int Width, int Height)
    {
        public const string MimeType = "image/jpeg";
    }

    private static PreviewImage DefaultImage(string siteUrl) =>
        new($"{siteUrl}/images/og-default.jpg", LocalFileStorageService.OgWidth, LocalFileStorageService.OgHeight);

    public static void MapSeoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/seo").WithTags("SEO (bots)");

        group.MapGet("/product/{slug}", async (string slug, IProductService service, IOptions<AppUrlOptions> appUrls, CancellationToken ct) =>
        {
            var siteUrl = appUrls.Value.PublicUrl.TrimEnd('/');

            try
            {
                var product = await service.GetBySlugAsync(slug, null, ct: ct);
                var description = string.IsNullOrWhiteSpace(product.Description)
                    ? $"{product.Name} — peça bordada do {SiteName}, feita sob medida com carinho."
                    : product.Description;
                var image = string.IsNullOrWhiteSpace(product.ImageUrl)
                    ? DefaultImage(siteUrl)
                    : new PreviewImage(
                        ToAbsolute(ImageVariants.ToOgUrl(product.ImageUrl), siteUrl),
                        LocalFileStorageService.OgWidth,
                        LocalFileStorageService.OgHeight);
                var url = $"{siteUrl}/produto/{product.Slug}";

                return Results.Text(BuildHtml(
                    title: $"{product.Name} — {SiteName}",
                    description: description,
                    image: image,
                    imageAlt: product.Name,
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
        image: DefaultImage(siteUrl),
        imageAlt: SiteName,
        url: siteUrl,
        type: "website",
        bodyHeading: SiteName,
        bodyText: DefaultDescription);

    private static string ToAbsolute(string url, string siteUrl) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{siteUrl}{url}";

    private static string BuildHtml(string title, string description, PreviewImage image, string imageAlt, string url, string type, string bodyHeading, string bodyText)
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
              <meta property="og:image" content="{{E(image.Url)}}">
              <meta property="og:image:secure_url" content="{{E(image.Url)}}">
              <meta property="og:image:type" content="{{PreviewImage.MimeType}}">
              <meta property="og:image:width" content="{{image.Width}}">
              <meta property="og:image:height" content="{{image.Height}}">
              <meta property="og:image:alt" content="{{E(imageAlt)}}">
              <meta property="og:site_name" content="{{E(SiteName)}}">
              <meta property="og:locale" content="pt_BR">

              <meta name="twitter:card" content="summary_large_image">
              <meta name="twitter:title" content="{{E(title)}}">
              <meta name="twitter:description" content="{{E(description)}}">
              <meta name="twitter:image" content="{{E(image.Url)}}">
              <meta name="twitter:image:alt" content="{{E(imageAlt)}}">
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
