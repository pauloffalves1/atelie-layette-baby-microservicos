using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.Identity.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence.Repositories;

public sealed class AdminRepository : IAdminRepository
{
    private readonly IdentityDbContext _dbContext;

    public AdminRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<Admin?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = Email.Create(email);
        return _dbContext.Admins.FirstOrDefaultAsync(a => a.Email == normalized, ct);
    }

    public Task<Admin?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<Admin>> ListAllAsync(CancellationToken ct = default) =>
        _dbContext.Admins.OrderBy(a => a.Name).ToListAsync(ct);

    public void Add(Admin admin) => _dbContext.Admins.Add(admin);
    public void Remove(Admin admin) => _dbContext.Admins.Remove(admin);
}
