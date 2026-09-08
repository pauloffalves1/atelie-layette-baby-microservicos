using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Application.SiteImages;

public sealed class SiteImageService : ISiteImageService
{
    private readonly ICatalogUnitOfWork _unitOfWork;

    public SiteImageService(ICatalogUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<SiteImageDto>> ListAsync(CancellationToken ct = default)
    {
        var images = await _unitOfWork.SiteImages.ListAsync(ct);
        return images.Select(ToDto).ToList();
    }

    public async Task<SiteImageDto> SetImageAsync(string key, string url, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.SiteImages.GetByKeyAsync(key, ct);

        if (existing is null)
        {
            existing = SiteImage.Create(key, url);
            _unitOfWork.SiteImages.Add(existing);
        }
        else
        {
            existing.UpdateUrl(url);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    private static SiteImageDto ToDto(SiteImage s) => new(s.Key, s.Url, s.UpdatedAt);
}
