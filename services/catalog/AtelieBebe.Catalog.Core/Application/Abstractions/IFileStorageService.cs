namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Persists an uploaded file to disk and returns the public URL it can be served from.</summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Brings an upload saved before photos were optimized up to the current variants (WebP + small copy),
    /// keeping the original file. Returns the URL to store from now on (the same one if nothing changed),
    /// or null when the URL is not a local upload or its file is missing.
    /// </summary>
    Task<string?> OptimizeExistingAsync(string url, CancellationToken ct = default);

    /// <summary>Best-effort delete of a previously saved file, given the public URL SaveAsync returned.</summary>
    Task DeleteAsync(string url, CancellationToken ct = default);
}
