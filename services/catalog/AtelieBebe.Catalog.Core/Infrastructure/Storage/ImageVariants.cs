namespace AtelieBebe.Catalog.Core.Infrastructure.Storage;

/// <summary>
/// Naming of the stored photo variants: "/api/uploads/products/abc.webp" is the full image and
/// "/api/uploads/products/abc-sm.webp" its small copy. The storefront derives the small URL the same
/// way (shared-src asset-url.ts), so no extra field is needed anywhere the URL is stored.
/// </summary>
public static class ImageVariants
{
    public const string Extension = ".webp";
    public const string SmallSuffix = "-sm";

    public static bool IsWebp(string url) => url.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    public static bool IsSmallVariant(string url) => url.EndsWith(SmallSuffix + Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>"x.jpg" → "x.webp" (the optimized full image written next to a legacy upload).</summary>
    public static string ToWebpUrl(string url)
    {
        var dot = url.LastIndexOf('.');
        var slash = url.LastIndexOf('/');
        return (dot > slash ? url[..dot] : url) + Extension;
    }

    /// <summary>"x.webp" → "x-sm.webp"; other formats have no small copy and are returned unchanged.</summary>
    public static string ToSmallUrl(string url) =>
        IsWebp(url) && !IsSmallVariant(url) ? url[..^Extension.Length] + SmallSuffix + Extension : url;
}
