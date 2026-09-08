namespace AtelieBebe.Identity.Core.Application.Abstractions;

/// <summary>TOTP (RFC 6238) generation/validation for admin two-factor authentication.</summary>
public interface ITotpService
{
    /// <summary>A fresh random secret, Base32-encoded so it can be typed manually into an authenticator app.</summary>
    string GenerateSecret();

    /// <summary>An otpauth:// URI an authenticator app can import (typically rendered as a QR code, but also readable as plain text).</summary>
    string BuildOtpAuthUri(string secret, string accountName, string issuer);

    /// <summary>Validates a 6-digit code against the secret, tolerating one 30s step of clock drift either way.</summary>
    bool ValidateCode(string secret, string code);
}
