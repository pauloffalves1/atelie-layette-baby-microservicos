using AtelieBebe.Catalog.Core.Domain.Entities;

namespace AtelieBebe.Catalog.Core.Domain.Repositories;

public interface ISiteImageRepository
{
    Task<SiteImage?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SiteImage>> ListAsync(CancellationToken ct = default);
    void Add(SiteImage siteImage);
}
