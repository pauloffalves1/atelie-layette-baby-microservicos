using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AtelieBebe.Identity.Core.Application.Abstractions;

namespace AtelieBebe.Identity.Core.Infrastructure.Security;

/// <summary>
/// Hand-rolled RFC 6238 TOTP (the same algorithm Google Authenticator/Authy use) instead of a
/// NuGet package — the algorithm is small, stable, and well-specified, and this avoids pulling
/// in a dependency for a security-sensitive feature (see LocalFileStorageService's ImageSharp
/// licensing surprise for why we're wary of that here).
/// </summary>
public sealed class TotpService : ITotpService
{
    private const int SecretBytesLength = 20;
    private const int Digits = 6;
    private const int StepSeconds = 30;
    private const int DriftSteps = 1;

    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(SecretBytesLength));

    public string BuildOtpAuthUri(string secret, string accountName, string issuer)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        var encodedIssuer = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={encodedIssuer}&digits={Digits}&period={StepSeconds}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits || !code.All(char.IsDigit))
            return false;

        var secretBytes = Base32Decode(secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

        for (var drift = -DriftSteps; drift <= DriftSteps; drift++)
        {
            if (ComputeCode(secretBytes, counter + drift) == code)
                return true;
        }

        return false;
    }

    private static string ComputeCode(byte[] secret, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) |
                     ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString(new string('0', Digits));
    }

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder();
        int bitBuffer = 0, bitCount = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5)
            {
                bitCount -= 5;
                sb.Append(Base32Alphabet[(bitBuffer >> bitCount) & 0x1F]);
            }
        }

        if (bitCount > 0)
            sb.Append(Base32Alphabet[(bitBuffer << (5 - bitCount)) & 0x1F]);

        return sb.ToString();
    }

    private static byte[] Base32Decode(string base32)
    {
        var normalized = base32.Trim().ToUpperInvariant().Replace("=", "");
        var bytes = new List<byte>();
        int bitBuffer = 0, bitCount = 0;

        foreach (var c in normalized)
        {
            var index = Array.IndexOf(Base32Alphabet, c);
            if (index < 0) continue;

            bitBuffer = (bitBuffer << 5) | index;
            bitCount += 5;
            if (bitCount >= 8)
            {
                bitCount -= 8;
                bytes.Add((byte)((bitBuffer >> bitCount) & 0xFF));
            }
        }

        return bytes.ToArray();
    }
}
