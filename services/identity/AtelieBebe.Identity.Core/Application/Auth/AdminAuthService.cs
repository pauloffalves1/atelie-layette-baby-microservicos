using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.Identity.Core.Application.Auth;

public sealed class AdminAuthService : IAdminAuthService
{
    private const string Issuer = "Ateliê Layette Baby";

    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITotpService _totpService;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(IIdentityUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, ITotpService totpService, ILogger<AdminAuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _totpService = totpService;
        _logger = logger;
    }

    public async Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {Email}", nameof(LoginAsync), request.Email);
        try
        {
            var admin = await _unitOfWork.Admins.GetByEmailAsync(request.Email, ct);
            if (admin is null || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
                throw new UnauthorizedAppException("E-mail ou senha inválidos.");

            if (admin.TwoFactorEnabled)
            {
                _logger.LogInformation("Saindo de {Method}", nameof(LoginAsync));
                return new AdminLoginResponse(true, admin.Id, null);
            }

            var token = _jwtTokenGenerator.GenerateAdminToken(admin);
            _logger.LogInformation("Saindo de {Method}", nameof(LoginAsync));
            return new AdminLoginResponse(false, null, new AuthResponse(token, admin.Id, admin.Name, admin.Email.Value, admin.Permissions.ToPermissionStrings()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(LoginAsync));
            throw;
        }
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(VerifyAdminTwoFactorRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(VerifyTwoFactorAsync), request.AdminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(request.AdminId, ct);
            if (admin is null || !admin.TwoFactorEnabled || admin.TwoFactorSecret is null)
                throw new UnauthorizedAppException("Código inválido.");

            if (!_totpService.ValidateCode(admin.TwoFactorSecret, request.Code))
                throw new UnauthorizedAppException("Código inválido.");

            var token = _jwtTokenGenerator.GenerateAdminToken(admin);
            _logger.LogInformation("Saindo de {Method}", nameof(VerifyTwoFactorAsync));
            return new AuthResponse(token, admin.Id, admin.Name, admin.Email.Value, admin.Permissions.ToPermissionStrings());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(VerifyTwoFactorAsync));
            throw;
        }
    }

    public async Task<bool> IsTwoFactorEnabledAsync(Guid adminId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(IsTwoFactorEnabledAsync), adminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);
            _logger.LogInformation("Saindo de {Method}", nameof(IsTwoFactorEnabledAsync));
            return admin.TwoFactorEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(IsTwoFactorEnabledAsync));
            throw;
        }
    }

    public async Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(Guid adminId, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(BeginTwoFactorSetupAsync), adminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);

            var secret = _totpService.GenerateSecret();
            var otpAuthUri = _totpService.BuildOtpAuthUri(secret, admin.Email.Value, Issuer);
            _logger.LogInformation("Saindo de {Method}", nameof(BeginTwoFactorSetupAsync));
            return new TwoFactorSetupDto(secret, otpAuthUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(BeginTwoFactorSetupAsync));
            throw;
        }
    }

    public async Task EnableTwoFactorAsync(Guid adminId, EnableTwoFactorRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(EnableTwoFactorAsync), adminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);

            if (!_totpService.ValidateCode(request.Secret, request.Code))
                throw new ConflictException("Código inválido. Confira o horário do seu celular e tente de novo.");

            admin.EnableTwoFactor(request.Secret);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(EnableTwoFactorAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(EnableTwoFactorAsync));
            throw;
        }
    }

    public async Task DisableTwoFactorAsync(Guid adminId, DisableTwoFactorRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(DisableTwoFactorAsync), adminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);

            if (!_passwordHasher.Verify(request.Password, admin.PasswordHash))
                throw new UnauthorizedAppException("Senha incorreta.");

            admin.DisableTwoFactor();
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(DisableTwoFactorAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(DisableTwoFactorAsync));
            throw;
        }
    }

    /// <summary>Self-service — any admin can change their own password, no AdminManagement permission needed.</summary>
    public async Task ChangePasswordAsync(Guid adminId, ChangeAdminPasswordRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Entrando em {Method} para {AdminId}", nameof(ChangePasswordAsync), adminId);
        try
        {
            var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
                ?? throw new NotFoundException("Administrador", adminId);

            if (!_passwordHasher.Verify(request.CurrentPassword, admin.PasswordHash))
                throw new UnauthorizedAppException("Senha atual incorreta.");

            admin.ChangePassword(_passwordHasher.Hash(request.NewPassword));
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saindo de {Method}", nameof(ChangePasswordAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro em {Method}", nameof(ChangePasswordAsync));
            throw;
        }
    }
}
