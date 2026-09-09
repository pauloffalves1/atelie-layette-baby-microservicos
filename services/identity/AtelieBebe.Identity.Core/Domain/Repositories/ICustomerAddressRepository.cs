using AtelieBebe.Identity.Core.Domain.Entities;

namespace AtelieBebe.Identity.Core.Domain.Repositories;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(CustomerAddress address);
    void Remove(CustomerAddress address);
}
