using System.Xml.Linq;
using AtelieBebe.Backoffice.Core.Application.MerchantFeed;

namespace AtelieBebe.Backoffice.Core.Tests.Application;

public class GoogleMerchantFeedBuilderTests
{
    private static readonly XNamespace G = "http://base.google.com/ns/1.0";

    private static MerchantFeedProduct Product(
        string slug = "fralda-de-boca-avulso",
        string name = "Fralda de Boca avulso",
        string? description = "Fralda de boca em 100% algodão.",
        decimal price = 55.9m,
        decimal? effectivePrice = null,
        bool isOnPromotion = false,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        IReadOnlyList<string>? images = null) =>
        new(slug, name, description, "Fralda de Boca", price, effectivePrice ?? price, isOnPromotion, startsAt, endsAt,
            images ?? ["/api/uploads/products/a.webp"]);

    private static List<XElement> Items(string xml) => XDocument.Parse(xml).Root!.Element("channel")!.Elements("item").ToList();

    private static string Field(XElement item, string name) => item.Element(G + name)!.Value;

    [Fact]
    public void Build_ProducesAnRssItemWithEveryRequiredField()
    {
        var xml = GoogleMerchantFeedBuilder.Build("https://layettebaby.com.br/", [Product()]);

        var item = Assert.Single(Items(xml));
        Assert.Equal("fralda-de-boca-avulso", Field(item, "id"));
        Assert.Equal("Fralda de Boca avulso", Field(item, "title"));
        Assert.Equal("Fralda de boca em 100% algodão.", Field(item, "description"));
        Assert.Equal("https://layettebaby.com.br/produto/fralda-de-boca-avulso", Field(item, "link"));
        Assert.Equal("https://layettebaby.com.br/api/uploads/products/a.webp", Field(item, "image_link"));
        Assert.Equal("in_stock", Field(item, "availability"));
        Assert.Equal("55.90 BRL", Field(item, "price"));
        Assert.Equal("new", Field(item, "condition"));
        Assert.Equal("Ateliê Layette Baby", Field(item, "brand"));
        Assert.Equal("no", Field(item, "identifier_exists"));
        Assert.Equal("Fralda de Boca", Field(item, "product_type"));
        Assert.Null(item.Element(G + "sale_price"));
    }

    [Fact]
    public void Build_ListsExtraPhotosOnceAndKeepsAbsoluteUrls()
    {
        var product = Product(images: ["/api/uploads/products/a.webp", "https://cdn.exemplo.com/b.jpg", "/api/uploads/products/a.webp"]);

        var item = Assert.Single(Items(GoogleMerchantFeedBuilder.Build("https://layettebaby.com.br", [product])));

        Assert.Equal("https://layettebaby.com.br/api/uploads/products/a.webp", Field(item, "image_link"));
        Assert.Equal(["https://cdn.exemplo.com/b.jpg"], item.Elements(G + "additional_image_link").Select(e => e.Value));
    }

    [Fact]
    public void Build_AddsSalePriceAndItsWindowWhileOnPromotion()
    {
        var product = Product(price: 100m, effectivePrice: 90m, isOnPromotion: true,
            startsAt: new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc), endsAt: new DateTime(2026, 10, 15, 2, 59, 0, DateTimeKind.Utc));

        var item = Assert.Single(Items(GoogleMerchantFeedBuilder.Build("https://layettebaby.com.br", [product])));

        Assert.Equal("100.00 BRL", Field(item, "price"));
        Assert.Equal("90.00 BRL", Field(item, "sale_price"));
        Assert.Equal("2026-10-01T03:00Z/2026-10-15T02:59Z", Field(item, "sale_price_effective_date"));
    }

    [Fact]
    public void Build_FallsBackToTheNameWithoutADescriptionAndSkipsProductsWithoutPhotos()
    {
        var xml = GoogleMerchantFeedBuilder.Build("https://layettebaby.com.br",
            [Product(description: "  "), Product(slug: "sem-foto", images: [])]);

        var item = Assert.Single(Items(xml));
        Assert.Equal("Fralda de Boca avulso", Field(item, "description"));
    }

    [Fact]
    public void Build_EscapesXmlInText()
    {
        var item = Assert.Single(Items(GoogleMerchantFeedBuilder.Build("https://layettebaby.com.br",
            [Product(name: "Kit Ombro & Boca <bordado>")])));

        Assert.Equal("Kit Ombro & Boca <bordado>", Field(item, "title"));
    }
}
