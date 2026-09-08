using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Domain.Repositories;

public interface IGalleryImageRepository
{
    Task<GalleryImage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GalleryImage>> ListAsync(CancellationToken ct = default);
    void Add(GalleryImage image);
    void Remove(GalleryImage image);
}
