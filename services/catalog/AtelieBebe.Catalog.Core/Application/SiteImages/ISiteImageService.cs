namespace AtelieBebe.Catalog.Core.Application.SiteImages;

public interface ISiteImageService
{
    Task<IReadOnlyList<SiteImageDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Single-image slots (e.g. "about"): replaces the existing image for the key, or
    /// creates the first one if none exists yet.</summary>
    Task<SiteImageDto> SetImageAsync(string key, string url, CancellationToken ct = default);

    /// <summary>Multi-image slots (e.g. "home-hero"): always appends a new image to the end of
    /// the key's carousel order.</summary>
    Task<SiteImageDto> AddImageAsync(string key, string url, CancellationToken ct = default);

    Task DeleteImageAsync(Guid id, CancellationToken ct = default);

    /// <summary>Swaps the image's SortOrder with its neighbor in the given direction, within the
    /// same key group. A no-op at either end of the list.</summary>
    Task MoveImageAsync(Guid id, MoveDirection direction, CancellationToken ct = default);
}
