namespace AtelieBebe.Identity.Core.Application.Auth;

public interface IAdminAuthService
{
    Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> VerifyTwoFactorAsync(VerifyAdminTwoFactorRequest request, CancellationToken ct = default);

    Task<bool> IsTwoFactorEnabledAsync(Guid adminId, CancellationToken ct = default);
    Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(Guid adminId, CancellationToken ct = default);
    Task EnableTwoFactorAsync(Guid adminId, EnableTwoFactorRequest request, CancellationToken ct = default);
    Task DisableTwoFactorAsync(Guid adminId, DisableTwoFactorRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid adminId, ChangeAdminPasswordRequest request, CancellationToken ct = default);
}
