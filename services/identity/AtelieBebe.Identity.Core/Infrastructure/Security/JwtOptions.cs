namespace AtelieBebe.Identity.Core.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Path to the RSA private key PEM file — only Identity ever reads this (mounted from a Secret/volume, never committed).</summary>
    public string PrivateKeyPath { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 120;
}
