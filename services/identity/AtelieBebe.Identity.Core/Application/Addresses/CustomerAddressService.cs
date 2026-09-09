using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Identity.Core.Application.Addresses;

public sealed class CustomerAddressService : ICustomerAddressService
{
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerAddressService> _logger;

    public CustomerAddressService(IIdentityUnitOfWork unitOfWork, ILogger<CustomerAddressService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> ListAsync(Guid customerId, CancellationToken ct = default)
    {
        var addresses = await _unitOfWork.CustomerAddresses.ListByCustomerAsync(customerId, ct);
        return addresses.Select(ToDto).ToList();
    }

    public async Task<CustomerAddressDto> CreateAsync(Guid customerId, SaveCustomerAddressRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(CreateAsync), customerId);
        try
        {
            var existing = await _unitOfWork.CustomerAddresses.ListByCustomerAsync(customerId, ct);
            var makeDefault = request.IsDefault || existing.Count == 0;

            var address = CustomerAddress.Create(customerId, request.Label, request.Street, request.Number, request.Complement,
                request.Neighborhood, request.City, request.State, request.ZipCode, makeDefault);

            if (makeDefault)
                foreach (var other in existing.Where(a => a.IsDefault))
                    other.UnmarkAsDefault();

            _unitOfWork.CustomerAddresses.Add(address);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(CreateAsync));
            return ToDto(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateAsync));
            throw;
        }
    }

    public async Task<CustomerAddressDto> UpdateAsync(Guid customerId, Guid addressId, SaveCustomerAddressRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}/{AddressId}", nameof(UpdateAsync), customerId, addressId);
        try
        {
            var address = await GetOwnedAsync(customerId, addressId, ct);

            address.Update(request.Label, request.Street, request.Number, request.Complement,
                request.Neighborhood, request.City, request.State, request.ZipCode);

            if (request.IsDefault && !address.IsDefault)
            {
                var existing = await _unitOfWork.CustomerAddresses.ListByCustomerAsync(customerId, ct);
                foreach (var other in existing.Where(a => a.IsDefault && a.Id != addressId))
                    other.UnmarkAsDefault();
                address.MarkAsDefault();
            }

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(UpdateAsync));
            return ToDto(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(UpdateAsync));
            throw;
        }
    }

    public async Task RemoveAsync(Guid customerId, Guid addressId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}/{AddressId}", nameof(RemoveAsync), customerId, addressId);
        try
        {
            var address = await GetOwnedAsync(customerId, addressId, ct);
            var wasDefault = address.IsDefault;
            _unitOfWork.CustomerAddresses.Remove(address);

            if (wasDefault)
            {
                var remaining = (await _unitOfWork.CustomerAddresses.ListByCustomerAsync(customerId, ct))
                    .Where(a => a.Id != addressId).ToList();
                remaining.FirstOrDefault()?.MarkAsDefault();
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(RemoveAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RemoveAsync));
            throw;
        }
    }

    public async Task<CustomerAddressDto> SetDefaultAsync(Guid customerId, Guid addressId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}/{AddressId}", nameof(SetDefaultAsync), customerId, addressId);
        try
        {
            var address = await GetOwnedAsync(customerId, addressId, ct);
            if (!address.IsDefault)
            {
                var existing = await _unitOfWork.CustomerAddresses.ListByCustomerAsync(customerId, ct);
                foreach (var other in existing.Where(a => a.IsDefault && a.Id != addressId))
                    other.UnmarkAsDefault();
                address.MarkAsDefault();
                await _unitOfWork.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Saindo de {Method}", nameof(SetDefaultAsync));
            return ToDto(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(SetDefaultAsync));
            throw;
        }
    }

    private async Task<CustomerAddress> GetOwnedAsync(Guid customerId, Guid addressId, CancellationToken ct)
    {
        var address = await _unitOfWork.CustomerAddresses.GetByIdAsync(addressId, ct)
            ?? throw new NotFoundException("Endereço", addressId);
        if (address.CustomerId != customerId)
            throw new NotFoundException("Endereço", addressId);
        return address;
    }

    private static CustomerAddressDto ToDto(CustomerAddress a) =>
        new(a.Id, a.Label, a.Street, a.Number, a.Complement, a.Neighborhood, a.City, a.State, a.ZipCode, a.IsDefault);
}
