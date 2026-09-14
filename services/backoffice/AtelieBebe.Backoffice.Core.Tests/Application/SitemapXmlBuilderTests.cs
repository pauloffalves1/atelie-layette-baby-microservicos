using System.Xml.Linq;
using AtelieBebe.Backoffice.Core.Application.Sitemap;

namespace AtelieBebe.Backoffice.Core.Tests.Application;

public class SitemapXmlBuilderTests
{
    private static readonly XNamespace Sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace Img = "http://www.google.com/schemas/sitemap-image/1.1";

    private static readonly SitemapProduct[] Products =
    [
        new("fralda-de-boca-florzinha", "Fralda de Boca", new DateTime(2026, 9, 10, 23, 30, 0, DateTimeKind.Utc),
            ["/api/uploads/products/a.jpg", "/api/uploads/products/b.jpg", "/api/uploads/products/a.jpg"]),
        new("kit-nuvem", "Kit Ombro e Boca", new DateTime(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc), []),
        new("fralda-de-boca-trico", "Fralda de Boca", new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            ["https://cdn.exemplo.com/c.jpg"]),
    ];

    private static List<XElement> Urls(string xml) => XDocument.Parse(xml).Root!.Elements(Sm + "url").ToList();

    private static XElement UrlFor(string xml, string loc) => Urls(xml).Single(u => u.Element(Sm + "loc")!.Value == loc);

    [Fact]
    public void Build_ProducesValidXmlWithStaticCategoryAndProductUrls()
    {
        var xml = SitemapXmlBuilder.Build("https://layettebaby.com.br/", Products);

        var locs = Urls(xml).Select(u => u.Element(Sm + "loc")!.Value).ToList();
        Assert.Equal(SitemapXmlBuilder.StaticPaths.Count + 2 + Products.Length, locs.Count);
        Assert.Contains("https://layettebaby.com.br/", locs);
        Assert.Contains("https://layettebaby.com.br/loja?categoria=Fralda%20de%20Boca", locs);
        Assert.Contains("https://layettebaby.com.br/loja?categoria=Kit%20Ombro%20e%20Boca", locs);
        Assert.Contains("https://layettebaby.com.br/produto/kit-nuvem", locs);
    }

    [Fact]
    public void Build_ProductLastmodIsItsUpdateDateAndCategoryUsesNewestProduct()
    {
        var xml = SitemapXmlBuilder.Build("https://layettebaby.com.br", Products);

        Assert.Equal("2026-09-10", UrlFor(xml, "https://layettebaby.com.br/produto/fralda-de-boca-florzinha").Element(Sm + "lastmod")!.Value);
        Assert.Equal("2026-09-10", UrlFor(xml, "https://layettebaby.com.br/loja?categoria=Fralda%20de%20Boca").Element(Sm + "lastmod")!.Value);
        Assert.Equal("2026-09-12", UrlFor(xml, "https://layettebaby.com.br/loja").Element(Sm + "lastmod")!.Value);
        Assert.Null(UrlFor(xml, "https://layettebaby.com.br/sobre").Element(Sm + "lastmod"));
    }

    [Fact]
    public void Build_ListsEachProductPhotoOnceAsAbsoluteImageUrl()
    {
        var xml = SitemapXmlBuilder.Build("https://layettebaby.com.br", Products);

        var images = UrlFor(xml, "https://layettebaby.com.br/produto/fralda-de-boca-florzinha")
            .Elements(Img + "image").Select(i => i.Element(Img + "loc")!.Value).ToList();
        Assert.Equal(["https://layettebaby.com.br/api/uploads/products/a.jpg", "https://layettebaby.com.br/api/uploads/products/b.jpg"], images);

        var external = UrlFor(xml, "https://layettebaby.com.br/produto/fralda-de-boca-trico").Elements(Img + "image").Single();
        Assert.Equal("https://cdn.exemplo.com/c.jpg", external.Element(Img + "loc")!.Value);
    }

    [Fact]
    public void Build_WithNoProducts_StillListsStaticPages()
    {
        var xml = SitemapXmlBuilder.Build("https://layettebaby.com.br", []);

        Assert.Equal(SitemapXmlBuilder.StaticPaths.Count, Urls(xml).Count);
    }
}
