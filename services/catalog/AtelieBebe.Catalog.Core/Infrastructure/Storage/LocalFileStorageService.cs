using AtelieBebe.Catalog.Core.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AtelieBebe.Catalog.Core.Infrastructure.Storage;

/// <summary>
/// Saves uploaded files to a local folder outside the app's publish output, so they survive a
/// redeploy (`dotnet publish` replaces the publish directory wholesale on every deploy).
/// Served back via app.UseStaticFiles under the same "Uploads:PublicPath" prefix (Program.cs).
/// Every caller uploads a photo (products/gallery/site/reviews, validated upstream by
/// ImageUploadValidator), so each one is decoded and written as WebP in two sizes
/// (<see cref="ImageVariants"/>): the full image, at most <see cref="MaxDimension"/> px, and a
/// "-sm" copy for cards and thumbnails. It used to keep the uploaded format, so a PNG photo went out
/// at 1.7 MB and every card downloaded the 1600 px original.
/// A third "-og.jpg" copy exists only for link previews: Facebook, Instagram and WhatsApp do not
/// decode WebP, and they will not draw a large card until they know the image's size, so the photo
/// is written as JPEG on a fixed <see cref="OgWidth"/>×<see cref="OgHeight"/> canvas — SeoEndpoints
/// then declares og:image:width/height and the very first share of a link unfurls immediately.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    public const int MaxDimension = 1600;
    public const int SmallDimension = 600;

    /// <summary>
    /// Square, because the photos are portrait: on the 1.91:1 canvas Facebook documents, a portrait
    /// photo is either cropped down to a strip across the embroidery or left floating between two
    /// wide white margins. Square is also what Instagram and WhatsApp draw natively.
    /// </summary>
    public const int OgWidth = 1200;
    public const int OgHeight = 1200;

    private const int WebpQuality = 80;
    private const int OgJpegQuality = 85;

    private readonly string _rootPath;
    private readonly string _publicBasePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _rootPath = configuration["Uploads:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _publicBasePath = configuration["Uploads:PublicPath"] ?? "/api/uploads";
    }

    public async Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct = default)
    {
        var folderPath = Path.Combine(_rootPath, folder);
        Directory.CreateDirectory(folderPath);

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        using var image = await Image.LoadAsync(content, ct);
        await WriteVariantsAsync(image, Path.Combine(folderPath, baseName), ct);

        return $"{_publicBasePath}/{folder}/{baseName}{ImageVariants.Extension}";
    }

    public async Task<string?> OptimizeExistingAsync(string url, CancellationToken ct = default)
    {
        var filePath = ToFilePath(url);
        if (filePath is null || !File.Exists(filePath) || ImageVariants.IsSmallVariant(url)) return null;

        var basePath = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath));
        var optimizedUrl = ImageVariants.ToWebpUrl(url);
        var smallMissing = !File.Exists(basePath + ImageVariants.SmallSuffix + ImageVariants.Extension);
        var ogMissing = !File.Exists(basePath + ImageVariants.OgSuffix + ImageVariants.OgExtension);
        if (ImageVariants.IsWebp(url) && !smallMissing && !ogMissing)
            return url; // already done

        using var image = await Image.LoadAsync(filePath, ct);
        if (ImageVariants.IsWebp(url))
        {
            // The full-size .webp is the file itself — leave it; write back only the copies that are
            // missing, so photos optimized before the "-og" copy existed pick it up on the next pass.
            if (smallMissing) await WriteSmallAsync(image, basePath, ct);
            if (ogMissing) await WriteOgAsync(image, basePath, ct);
        }
        else
        {
            await WriteVariantsAsync(image, basePath, ct); // original .jpg/.png stays for anything still linking to it
        }

        return optimizedUrl;
    }

    /// <summary>Writes "<paramref name="basePath"/>.webp" (≤ MaxDimension) and "-sm.webp" (≤ SmallDimension).</summary>
    public static async Task WriteVariantsAsync(Image image, string basePath, CancellationToken ct = default)
    {
        // Phones store the photo sideways with an orientation flag; browsers honour it inconsistently
        // once it's re-encoded, so bake the rotation in. EXIF/IPTC/XMP go away — they can carry the
        // GPS location of the ateliê or of a customer's home (review photos).
        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        ResizeToFit(image, MaxDimension);
        await image.SaveAsync(basePath + ImageVariants.Extension, new WebpEncoder { Quality = WebpQuality }, ct);
        await WriteSmallAsync(image, basePath, ct);
        await WriteOgAsync(image, basePath, ct);
    }

    private static async Task WriteSmallAsync(Image image, string basePath, CancellationToken ct)
    {
        using var small = image.Clone(_ => { });
        small.Mutate(x => x.AutoOrient());
        ResizeToFit(small, SmallDimension);
        await small.SaveAsync(basePath + ImageVariants.SmallSuffix + ImageVariants.Extension, new WebpEncoder { Quality = WebpQuality }, ct);
    }

    /// <summary>
    /// Writes "<paramref name="basePath"/>-og.jpg": the photo padded onto a white
    /// <see cref="OgWidth"/>×<see cref="OgHeight"/> canvas. Padded rather than cropped because the
    /// embroidery — the whole point of the shot — is not always centred, and a crop would cut it away.
    /// </summary>
    private static async Task WriteOgAsync(Image image, string basePath, CancellationToken ct)
    {
        using var og = image.Clone(x => x
            .AutoOrient()
            .Resize(new ResizeOptions
            {
                Mode = ResizeMode.Pad,
                Size = new Size(OgWidth, OgHeight),
                PadColor = Color.White,
            }));
        await og.SaveAsync(basePath + ImageVariants.OgSuffix + ImageVariants.OgExtension, new JpegEncoder { Quality = OgJpegQuality }, ct);
    }

    private static void ResizeToFit(Image image, int maxDimension)
    {
        if (image.Width <= maxDimension && image.Height <= maxDimension) return;
        image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxDimension, maxDimension) }));
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var filePath = ToFilePath(url);
        if (filePath is null) return Task.CompletedTask;

        if (File.Exists(filePath))
            File.Delete(filePath);

        foreach (var variantUrl in new[] { ImageVariants.ToSmallUrl(url), ImageVariants.ToOgUrl(url) })
        {
            var variantPath = ToFilePath(variantUrl);
            if (variantPath is not null && variantPath != filePath && File.Exists(variantPath))
                File.Delete(variantPath);
        }

        return Task.CompletedTask;
    }

    private string? ToFilePath(string url)
    {
        if (!url.StartsWith(_publicBasePath + "/", StringComparison.OrdinalIgnoreCase))
            return null;

        var relativePath = url[_publicBasePath.Length..].TrimStart('/');
        if (relativePath.Contains("..", StringComparison.Ordinal)) return null;
        return Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
