namespace AtelieBebe.Identity.Core.Application.Auth;

public sealed record RegisterCustomerRequest(
    string Name, string Email, string Cpf, string Password, string? Phone,
    string? AddressStreet = null, string? AddressNumber = null, string? AddressComplement = null,
    string? AddressNeighborhood = null, string? AddressCity = null, string? AddressState = null, string? AddressZipCode = null);

public sealed record LoginRequest(string Email, string Password);
public sealed record AdminLoginRequest(string Email, string Password);

public sealed record AuthResponse(string Token, Guid Id, string Name, string Email);

/// <summary>Either a completed login (Auth populated) or a request for the second 2FA step (RequiresTwoFactor + AdminId).</summary>
public sealed record AdminLoginResponse(bool RequiresTwoFactor, Guid? AdminId, AuthResponse? Auth);

public sealed record VerifyAdminTwoFactorRequest(Guid AdminId, string Code);

public sealed record TwoFactorSetupDto(string Secret, string OtpAuthUri);

public sealed record EnableTwoFactorRequest(string Secret, string Code);

public sealed record DisableTwoFactorRequest(string Password);

public sealed record CustomerProfileDto(
    Guid Id, string Name, string Email, string? Phone, string? Cpf,
    string? AddressStreet, string? AddressNumber, string? AddressComplement,
    string? AddressNeighborhood, string? AddressCity, string? AddressState, string? AddressZipCode,
    bool EmailVerified);

public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record DeleteAccountRequest(string Password);
public sealed record VerifyEmailRequest(string Token);
