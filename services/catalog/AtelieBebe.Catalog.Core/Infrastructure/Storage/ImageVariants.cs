namespace AtelieBebe.Catalog.Core.Infrastructure.Storage;

/// <summary>
/// Naming of the stored photo variants: "/api/uploads/products/abc.webp" is the full image,
/// "/api/uploads/products/abc-sm.webp" its small copy, and "/api/uploads/products/abc-og.jpg" the
/// copy link-preview bots get (Facebook/Instagram/WhatsApp never render WebP — a shared link with a
/// WebP og:image shows an empty grey box). The storefront derives the small URL the same way
/// (shared-src asset-url.ts), so no extra field is needed anywhere the URL is stored.
/// </summary>
public static class ImageVariants
{
    public const string Extension = ".webp";
    public const string SmallSuffix = "-sm";

    /// <summary>Link previews are JPEG: Facebook, Instagram and WhatsApp do not decode WebP.</summary>
    public const string OgSuffix = "-og";
    public const string OgExtension = ".jpg";

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

    /// <summary>"x.webp" → "x-og.jpg"; anything else (already JPEG/PNG, or the small copy) is returned unchanged.</summary>
    public static string ToOgUrl(string url) =>
        IsWebp(url) && !IsSmallVariant(url) ? url[..^Extension.Length] + OgSuffix + OgExtension : url;
}
