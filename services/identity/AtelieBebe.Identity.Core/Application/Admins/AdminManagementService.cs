using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Identity.Core.Application.Admins;

/// <summary>
/// Lets an admin holding <see cref="AdminPermission.AdminManagement"/> register more admins and
/// choose exactly which feature areas each one can use — see the per-permission JWT claims/policies
/// in <see cref="AtelieBebe.SharedKernel.Auth.JwtAuthenticationExtensions"/> for how a granted
/// permission actually gates an endpoint. Changing your own password or 2FA is handled separately
/// by <see cref="Auth.IAdminAuthService"/> — no AdminManagement permission needed for that, since
/// every admin manages those for themselves.
/// </summary>
public sealed class AdminManagementService : IAdminManagementService
{
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminManagementService> _logger;

    public AdminManagementService(IIdentityUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger<AdminManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdminSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method}", nameof(ListAsync));
        try
        {
            var admins = await _unitOfWork.Admins.ListAllAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(ListAsync));
            return admins.Select(ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ListAsync));
            throw;
        }
    }

    public async Task<AdminSummaryDto> CreateAsync(CreateAdminRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {Email}", nameof(CreateAsync), request.Email);
        try
        {
            var email = Email.Create(request.Email);
            if (await _unitOfWork.Admins.GetByEmailAsync(request.Email, ct) is not null)
                throw new ConflictException("Já existe um administrador com este e-mail.");

            var permissions = request.Permissions.ParsePermissions();
            var admin = Admin.Create(request.Name, email, _passwordHasher.Hash(request.Password), permissions);

            _unitOfWork.Admins.Add(admin);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(CreateAsync));
            return ToDto(admin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(CreateAsync));
            throw;
        }
    }

    public async Task<AdminSummaryDto> UpdatePermissionsAsync(Guid adminId, UpdateAdminPermissionsRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(UpdatePermissionsAsync), adminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);

            var newPermissions = request.Permissions.ParsePermissions();

            if (admin.Permissions.HasFlag(AdminPermission.AdminManagement) && !newPermissions.HasFlag(AdminPermission.AdminManagement))
                await EnsureNotLastAdminManagerAsync(adminId, ct);

            admin.UpdatePermissions(newPermissions);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(UpdatePermissionsAsync));
            return ToDto(admin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(UpdatePermissionsAsync));
            throw;
        }
    }

    public async Task<AdminSummaryDto> RemoveAsync(Guid adminId, Guid requestedByAdminId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(RemoveAsync), adminId);
        try
        {
            if (adminId == requestedByAdminId)
                throw new ConflictException("Você não pode remover sua própria conta de administrador.");

            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);

            if (admin.Permissions.HasFlag(AdminPermission.AdminManagement))
                await EnsureNotLastAdminManagerAsync(adminId, ct);

            var dto = ToDto(admin);
            _unitOfWork.Admins.Remove(admin);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Saindo de {Method}", nameof(RemoveAsync));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(RemoveAsync));
            throw;
        }
    }

    /// <summary>Guards against ever ending up with zero admins able to manage other admins — checked before revoking AdminManagement from, or deleting, whoever currently holds it.</summary>
    private async Task EnsureNotLastAdminManagerAsync(Guid excludingAdminId, CancellationToken ct)
    {
        var admins = await _unitOfWork.Admins.ListAllAsync(ct);
        var otherManagers = admins.Where(a => a.Id != excludingAdminId && a.Permissions.HasFlag(AdminPermission.AdminManagement));
        if (!otherManagers.Any())
            throw new ConflictException("Precisa haver ao menos um administrador com a permissão de Gerenciar Administradores.");
    }

    private static AdminSummaryDto ToDto(Admin a) =>
        new(a.Id, a.Name, a.Email.Value, a.TwoFactorEnabled, a.Permissions.ToPermissionStrings(), a.CreatedAt);
}
