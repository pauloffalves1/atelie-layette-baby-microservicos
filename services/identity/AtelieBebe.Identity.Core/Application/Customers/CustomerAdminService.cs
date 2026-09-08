using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Identity.Core.Application.Customers;

public sealed class CustomerAdminService : ICustomerAdminService
{
    private readonly IIdentityUnitOfWork _unitOfWork;

    public CustomerAdminService(IIdentityUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var customers = await _unitOfWork.Customers.ListAsync(ct);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerSummaryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cliente", id);
        return ToDto(customer);
    }

    public async Task<CustomerSummaryDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cliente", id);

        if (customer.IsAnonymized)
            throw new ConflictException("Esta conta foi excluída pelo cliente e não pode mais ser editada.");

        var email = Email.Create(request.Email);
        var existingByEmail = await _unitOfWork.Customers.GetByEmailAsync(request.Email, ct);
        if (existingByEmail is not null && existingByEmail.Id != id)
            throw new ConflictException("Já existe uma conta com este e-mail.");

        var cpf = Cpf.Create(request.Cpf);
        var existingByCpf = await _unitOfWork.Customers.GetByCpfAsync(request.Cpf, ct);
        if (existingByCpf is not null && existingByCpf.Id != id)
            throw new ConflictException("Já existe uma conta com este CPF.");

        customer.UpdateDetails(
            request.Name, email, cpf, request.Phone,
            request.AddressStreet, request.AddressNumber, request.AddressComplement,
            request.AddressNeighborhood, request.AddressCity, request.AddressState, request.AddressZipCode);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    private static CustomerSummaryDto ToDto(Customer c) =>
        new(
            c.Id, c.Name, c.Email.Value, c.Phone, c.Cpf?.Value, c.CreatedAt, c.IsAnonymized,
            c.AddressStreet, c.AddressNumber, c.AddressComplement, c.AddressNeighborhood, c.AddressCity, c.AddressState, c.AddressZipCode);
}
