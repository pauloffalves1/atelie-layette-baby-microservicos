namespace AtelieBebe.Catalog.Core.Application.Gallery;

public interface IGalleryImageService
{
    Task<IReadOnlyList<GalleryImageDto>> ListAsync(CancellationToken ct = default);
    Task<GalleryImageDto> AddAsync(string url, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
