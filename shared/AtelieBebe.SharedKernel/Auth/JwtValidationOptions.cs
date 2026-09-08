namespace AtelieBebe.SharedKernel.Auth;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Path to the RSA public key PEM file — every service except Identity only ever has this half of the key pair.</summary>
    public string PublicKeyPath { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
}
