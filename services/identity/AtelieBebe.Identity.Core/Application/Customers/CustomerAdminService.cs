using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Identity.Core.Application.Customers;

public sealed class CustomerAdminService : ICustomerAdminService
{
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOrdersServiceClient _ordersServiceClient;
    private readonly ILogger<CustomerAdminService> _logger;

    public CustomerAdminService(IIdentityUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IOrdersServiceClient ordersServiceClient, ILogger<CustomerAdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _ordersServiceClient = ordersServiceClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var customers = await _unitOfWork.Customers.ListAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return customers.Select(ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }

    public async Task<CustomerSummaryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(GetByIdAsync), id);
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Cliente", id);
            _logger.LogInformation("Saindo de {Method}", nameof(GetByIdAsync));
            return ToDto(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<CustomerSummaryDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(UpdateAsync), id);
        try
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

            _logger.LogInformation("Saindo de {Method}", nameof(UpdateAsync));
            return ToDto(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(UpdateAsync));
            throw;
        }
    }

    public async Task<CustomerSummaryDto> VerifyEmailAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(VerifyEmailAsync), id);
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Cliente", id);

            if (customer.IsAnonymized)
                throw new ConflictException("Esta conta foi excluída pelo cliente e não pode mais ser editada.");

            if (!customer.EmailVerified)
            {
                customer.VerifyEmail();
                await _unitOfWork.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Saindo de {Method}", nameof(VerifyEmailAsync));
            return ToDto(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(VerifyEmailAsync));
            throw;
        }
    }

    /// <summary>
    /// Admin-triggered account removal — same LGPD-safe rule as the customer's own self-service
    /// deletion (<see cref="Auth.CustomerAuthService.DeleteAccountAsync"/>): hard-delete the row when
    /// there's no order history to preserve, otherwise scrub personal data via <see cref="Customer.Anonymize"/>
    /// so past orders keep their own point-in-time snapshot.
    /// </summary>
    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {CustomerId}", nameof(RemoveAsync), id);
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Cliente", id);

            if (customer.IsAnonymized)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(RemoveAsync));
                return;
            }

            var hasOrders = await _ordersServiceClient.CustomerHasOrdersAsync(id, ct);
            if (!hasOrders)
                _unitOfWork.Customers.Remove(customer);
            else
                customer.Anonymize(_passwordHasher.Hash(Guid.NewGuid().ToString("N")));

            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(RemoveAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RemoveAsync));
            throw;
        }
    }

    private static CustomerSummaryDto ToDto(Customer c) =>
        new(
            c.Id, c.Name, c.Email.Value, c.Phone, c.Cpf?.Value, c.CreatedAt, c.IsAnonymized, c.EmailVerified,
            c.AddressStreet, c.AddressNumber, c.AddressComplement, c.AddressNeighborhood, c.AddressCity, c.AddressState, c.AddressZipCode);
}
