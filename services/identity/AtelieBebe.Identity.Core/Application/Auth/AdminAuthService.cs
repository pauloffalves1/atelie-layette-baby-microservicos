using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Exceptions;

namespace AtelieBebe.Identity.Core.Application.Auth;

public sealed class AdminAuthService : IAdminAuthService
{
    private const string Issuer = "Ateliê Layette Baby";

    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITotpService _totpService;

    public AdminAuthService(IIdentityUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, ITotpService totpService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _totpService = totpService;
    }

    public async Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default)
    {
        var admin = await _unitOfWork.Admins.GetByEmailAsync(request.Email, ct);
        if (admin is null || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedAppException("E-mail ou senha inválidos.");

        if (admin.TwoFactorEnabled)
            return new AdminLoginResponse(true, admin.Id, null);

        var token = _jwtTokenGenerator.GenerateAdminToken(admin);
        return new AdminLoginResponse(false, null, new AuthResponse(token, admin.Id, admin.Name, admin.Email.Value));
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(VerifyAdminTwoFactorRequest request, CancellationToken ct = default)
    {
        var admin = await _unitOfWork.Admins.GetByIdAsync(request.AdminId, ct);
        if (admin is null || !admin.TwoFactorEnabled || admin.TwoFactorSecret is null)
            throw new UnauthorizedAppException("Código inválido.");

        if (!_totpService.ValidateCode(admin.TwoFactorSecret, request.Code))
            throw new UnauthorizedAppException("Código inválido.");

        var token = _jwtTokenGenerator.GenerateAdminToken(admin);
        return new AuthResponse(token, admin.Id, admin.Name, admin.Email.Value);
    }

    public async Task<bool> IsTwoFactorEnabledAsync(Guid adminId, CancellationToken ct = default)
    {
        var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
            ?? throw new NotFoundException("Administrador", adminId);
        return admin.TwoFactorEnabled;
    }

    public async Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(Guid adminId, CancellationToken ct = default)
    {
        var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
            ?? throw new NotFoundException("Administrador", adminId);

        var secret = _totpService.GenerateSecret();
        var otpAuthUri = _totpService.BuildOtpAuthUri(secret, admin.Email.Value, Issuer);
        return new TwoFactorSetupDto(secret, otpAuthUri);
    }

    public async Task EnableTwoFactorAsync(Guid adminId, EnableTwoFactorRequest request, CancellationToken ct = default)
    {
        var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
            ?? throw new NotFoundException("Administrador", adminId);

        if (!_totpService.ValidateCode(request.Secret, request.Code))
            throw new ConflictException("Código inválido. Confira o horário do seu celular e tente de novo.");

        admin.EnableTwoFactor(request.Secret);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DisableTwoFactorAsync(Guid adminId, DisableTwoFactorRequest request, CancellationToken ct = default)
    {
        var admin = await _unitOfWork.Admins.GetByIdAsync(adminId, ct)
            ?? throw new NotFoundException("Administrador", adminId);

        if (!_passwordHasher.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedAppException("Senha incorreta.");

        admin.DisableTwoFactor();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
