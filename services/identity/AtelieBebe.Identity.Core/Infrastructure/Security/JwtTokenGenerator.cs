using System.Security.Claims;
using System.Security.Cryptography;
using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AtelieBebe.Identity.Core.Infrastructure.Security;

/// <summary>
/// Signs with an RSA private key (RS256) instead of the monolith's shared HMAC secret — the
/// microservices' whole point is that only Identity holds the private key, while every other
/// service (Catalog/Orders/Backoffice/Gateway) only needs the matching public key to validate a
/// token, never able to forge one. The private key PEM is loaded once at startup.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly RSA _privateKey;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(File.ReadAllText(_options.PrivateKeyPath));
    }

    public string GenerateCustomerToken(Customer customer) =>
        GenerateToken(customer.Id, customer.Name, customer.Email.Value, Roles.Customer);

    public string GenerateAdminToken(Admin admin) =>
        GenerateToken(admin.Id, admin.Name, admin.Email.Value, Roles.Admin);

    private string GenerateToken(Guid id, string name, string email, string role)
    {
        var key = new RsaSecurityKey(_privateKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
            }),
        };

        return _handler.CreateToken(descriptor);
    }
}
