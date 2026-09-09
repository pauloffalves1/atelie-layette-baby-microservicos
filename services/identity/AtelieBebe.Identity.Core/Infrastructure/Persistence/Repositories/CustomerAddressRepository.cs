using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.Identity.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence.Repositories;

public sealed class CustomerAddressRepository : ICustomerAddressRepository
{
    private readonly IdentityDbContext _dbContext;

    public CustomerAddressRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CustomerAddress>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await _dbContext.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id, ct);

    public void Add(CustomerAddress address) => _dbContext.CustomerAddresses.Add(address);

    public void Remove(CustomerAddress address) => _dbContext.CustomerAddresses.Remove(address);
}
