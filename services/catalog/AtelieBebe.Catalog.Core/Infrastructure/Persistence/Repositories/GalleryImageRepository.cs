using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;

public sealed class GalleryImageRepository : IGalleryImageRepository
{
    private readonly CatalogDbContext _dbContext;

    public GalleryImageRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public Task<GalleryImage?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.GalleryImages.FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IReadOnlyList<GalleryImage>> ListAsync(CancellationToken ct = default) =>
        await _dbContext.GalleryImages.OrderByDescending(g => g.CreatedAt).ToListAsync(ct);

    public void Add(GalleryImage image) => _dbContext.GalleryImages.Add(image);

    public void Remove(GalleryImage image) => _dbContext.GalleryImages.Remove(image);
}
