using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.Identity.Core.Domain.Repositories;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IdentityDbContext _dbContext;

    public CustomerRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = Email.Create(email);
        return _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == normalized, ct);
    }

    public Task<Customer?> GetByCpfAsync(string cpf, CancellationToken ct = default)
    {
        var normalized = Cpf.Create(cpf);
        return _dbContext.Customers.FirstOrDefaultAsync(c => c.Cpf == normalized, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = Email.Create(email);
        return _dbContext.Customers.AnyAsync(c => c.Email == normalized, ct);
    }

    public Task<bool> CpfExistsAsync(string cpf, CancellationToken ct = default)
    {
        var normalized = Cpf.Create(cpf);
        return _dbContext.Customers.AnyAsync(c => c.Cpf == normalized, ct);
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default) =>
        await _dbContext.Customers.OrderBy(c => c.Name).ToListAsync(ct);

    public void Add(Customer customer) => _dbContext.Customers.Add(customer);

    public void Remove(Customer customer) => _dbContext.Customers.Remove(customer);
}
