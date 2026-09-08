namespace AtelieBebe.Catalog.Core.Application.SiteImages;

public interface ISiteImageService
{
    Task<IReadOnlyList<SiteImageDto>> ListAsync(CancellationToken ct = default);
    Task<SiteImageDto> SetImageAsync(string key, string url, CancellationToken ct = default);
}
