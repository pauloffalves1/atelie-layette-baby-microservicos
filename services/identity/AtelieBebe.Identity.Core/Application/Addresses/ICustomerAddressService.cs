namespace AtelieBebe.Identity.Core.Application.Addresses;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddressDto>> ListAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerAddressDto> CreateAsync(Guid customerId, SaveCustomerAddressRequest request, CancellationToken ct = default);
    Task<CustomerAddressDto> UpdateAsync(Guid customerId, Guid addressId, SaveCustomerAddressRequest request, CancellationToken ct = default);
    Task RemoveAsync(Guid customerId, Guid addressId, CancellationToken ct = default);
    Task<CustomerAddressDto> SetDefaultAsync(Guid customerId, Guid addressId, CancellationToken ct = default);
}
