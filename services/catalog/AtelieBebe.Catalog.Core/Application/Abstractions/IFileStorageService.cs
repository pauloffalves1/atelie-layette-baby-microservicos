namespace AtelieBebe.Catalog.Core.Application.Abstractions;

/// <summary>Persists an uploaded file to disk and returns the public URL it can be served from.</summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>Best-effort delete of a previously saved file, given the public URL SaveAsync returned.</summary>
    Task DeleteAsync(string url, CancellationToken ct = default);
}
