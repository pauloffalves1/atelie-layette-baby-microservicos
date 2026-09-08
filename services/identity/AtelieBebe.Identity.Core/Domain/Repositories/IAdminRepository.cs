using AtelieBebe.Identity.Core.Domain.Entities;

namespace AtelieBebe.Identity.Core.Domain.Repositories;

public interface IAdminRepository
{
    Task<Admin?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Admin?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Admin admin);
}
