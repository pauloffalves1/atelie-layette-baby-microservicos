using AtelieBebe.Catalog.Core.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AtelieBebe.Catalog.Core.Tests.Infrastructure;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "atelie-uploads-" + Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorageService _storage;

    public LocalFileStorageServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Path"] = _root, ["Uploads:PublicPath"] = "/api/uploads" })
            .Build();
        _storage = new LocalFileStorageService(configuration);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static MemoryStream PngStream(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(232, 162, 173));
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task SaveAsync_WritesWebpFullAndSmallVariantsWithinTheirSizes()
    {
        var url = await _storage.SaveAsync("products", "foto.png", PngStream(3200, 1600));

        Assert.Equal("/api/uploads/products/foto.webp", url);
        using var full = await Image.LoadAsync(Path.Combine(_root, "products", "foto.webp"));
        using var small = await Image.LoadAsync(Path.Combine(_root, "products", "foto-sm.webp"));
        Assert.Equal((1600, 800), (full.Width, full.Height));
        Assert.Equal((600, 300), (small.Width, small.Height));
        Assert.False(File.Exists(Path.Combine(_root, "products", "foto.png")));
    }

    [Fact]
    public async Task SaveAsync_DoesNotUpscaleSmallPhotos()
    {
        await _storage.SaveAsync("reviews", "mini.jpg", PngStream(400, 300));

        using var full = await Image.LoadAsync(Path.Combine(_root, "reviews", "mini.webp"));
        using var small = await Image.LoadAsync(Path.Combine(_root, "reviews", "mini-sm.webp"));
        Assert.Equal((400, 300), (full.Width, full.Height));
        Assert.Equal((400, 300), (small.Width, small.Height));
    }

    [Fact]
    public async Task OptimizeExistingAsync_ConvertsLegacyUploadKeepsOriginalAndIsIdempotent()
    {
        Directory.CreateDirectory(Path.Combine(_root, "site"));
        await using (var file = File.Create(Path.Combine(_root, "site", "hero.png")))
            await PngStream(2000, 2000).CopyToAsync(file);

        var first = await _storage.OptimizeExistingAsync("/api/uploads/site/hero.png");
        var second = await _storage.OptimizeExistingAsync("/api/uploads/site/hero.webp");

        Assert.Equal("/api/uploads/site/hero.webp", first);
        Assert.Equal("/api/uploads/site/hero.webp", second);
        Assert.True(File.Exists(Path.Combine(_root, "site", "hero.png")));
        Assert.True(File.Exists(Path.Combine(_root, "site", "hero-sm.webp")));
    }

    [Theory]
    [InlineData("https://picsum.photos/600")]
    [InlineData("/api/uploads/products/nao-existe.jpg")]
    [InlineData("/api/uploads/../secrets.png")]
    public async Task OptimizeExistingAsync_IgnoresExternalMissingOrUnsafeUrls(string url)
    {
        Assert.Null(await _storage.OptimizeExistingAsync(url));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheSmallVariantToo()
    {
        var url = await _storage.SaveAsync("gallery", "g.jpg", PngStream(800, 800));

        await _storage.DeleteAsync(url);

        Assert.False(File.Exists(Path.Combine(_root, "gallery", "g.webp")));
        Assert.False(File.Exists(Path.Combine(_root, "gallery", "g-sm.webp")));
    }

    [Theory]
    [InlineData("/api/uploads/products/a.webp", "/api/uploads/products/a-sm.webp")]
    [InlineData("/api/uploads/products/a-sm.webp", "/api/uploads/products/a-sm.webp")]
    [InlineData("/api/uploads/products/a.jpg", "/api/uploads/products/a.jpg")]
    public void ImageVariants_ToSmallUrl(string url, string expected)
    {
        Assert.Equal(expected, ImageVariants.ToSmallUrl(url));
    }
}
