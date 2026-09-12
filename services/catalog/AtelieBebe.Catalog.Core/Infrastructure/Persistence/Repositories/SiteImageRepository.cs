using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;

public sealed class SiteImageRepository : ISiteImageRepository
{
    private readonly CatalogDbContext _dbContext;

    public SiteImageRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public Task<SiteImage?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        _dbContext.SiteImages.Where(s => s.Key == key).OrderBy(s => s.SortOrder).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SiteImage>> ListByKeyAsync(string key, CancellationToken ct = default) =>
        await _dbContext.SiteImages.Where(s => s.Key == key).OrderBy(s => s.SortOrder).ToListAsync(ct);

    public Task<SiteImage?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.SiteImages.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<SiteImage>> ListAsync(CancellationToken ct = default) =>
        await _dbContext.SiteImages.OrderBy(s => s.Key).ThenBy(s => s.SortOrder).ToListAsync(ct);

    public void Add(SiteImage siteImage) => _dbContext.SiteImages.Add(siteImage);

    public void Remove(SiteImage siteImage) => _dbContext.SiteImages.Remove(siteImage);
}
