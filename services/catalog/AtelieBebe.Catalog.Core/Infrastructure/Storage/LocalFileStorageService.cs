using AtelieBebe.Catalog.Core.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AtelieBebe.Catalog.Core.Infrastructure.Storage;

/// <summary>
/// Saves uploaded files to a local folder outside the app's publish output, so they survive a
/// redeploy (`dotnet publish` replaces the publish directory wholesale on every deploy).
/// Served back via app.UseStaticFiles under the same "Uploads:PublicPath" prefix (Program.cs).
/// Every caller of this service uploads an image (products/gallery/site photos, validated
/// upstream by ImageUploadValidator), so SaveAsync always decodes and re-encodes through
/// ImageSharp — downscaling anything above MaxDimension and re-compressing — instead of writing
/// the raw bytes straight to disk.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private const int MaxDimension = 1600;
    private const int JpegQuality = 82;
    private const int WebpQuality = 82;

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

        var filePath = Path.Combine(folderPath, fileName);

        using var image = await Image.LoadAsync(content, ct);
        if (image.Width > MaxDimension || image.Height > MaxDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxDimension, MaxDimension),
            }));
        }

        await image.SaveAsync(filePath, GetEncoder(fileName), ct);

        return $"{_publicBasePath}/{folder}/{fileName}";
    }

    private static SixLabors.ImageSharp.Formats.IImageEncoder GetEncoder(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
            ".webp" => new WebpEncoder { Quality = WebpQuality },
            _ => new JpegEncoder { Quality = JpegQuality },
        };

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith(_publicBasePath, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var relativePath = url[_publicBasePath.Length..].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(_rootPath, relativePath);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
