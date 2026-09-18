using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AtelieBebe.SharedKernel.Auth;

/// <summary>
/// Wires up JWT bearer validation against Identity's RSA public key, plus the same "AdminOnly"/
/// "CustomerOnly" policies the monolith had — called by every service that authorizes its own
/// endpoints (Catalog, Orders, Backoffice) and by the Gateway (so it can reject bad tokens before
/// even proxying the request). Identity itself does NOT call this — it validates nothing, only
/// signs (see AtelieBebe.Identity.Core.Infrastructure.Security.JwtTokenGenerator).
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>Claim type Identity issues one instance of per permission an admin holds.</summary>
    public const string PermissionClaimType = "permission";

    /// <summary>
    /// Claim Identity puts on a test customer's token (RF40). It travels in the token for the same
    /// reason permissions do — Catalog and Orders decide what a test customer may see and what
    /// counts as a test purchase without calling Identity back on every request. Approving or
    /// revoking a test customer therefore only takes effect on her next login.
    /// </summary>
    public const string TestUserClaimType = "test_user";

    /// <summary>The authorization policy name for a given permission — e.g. "Admin.Products".</summary>
    public static string PermissionPolicyName(AdminPermission permission) => $"Admin.{permission}";

    public static IServiceCollection AddSharedJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>()
            ?? throw new InvalidOperationException("Configuração 'Jwt' ausente em appsettings.json.");

        var publicKey = RSA.Create();
        publicKey.ImportFromPem(File.ReadAllText(options.PublicKeyPath));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearerOptions =>
            {
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(publicKey),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        var authorizationBuilder = services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin))
            .AddPolicy("CustomerOnly", policy => policy.RequireRole(Roles.Customer));

        // One policy per AdminPermission flag ("Admin.Products", "Admin.Orders", ...) — an admin
        // endpoint group requires the specific flag for its feature instead of blanket "AdminOnly",
        // so permissions can be granted per admin (see PermissionClaimType/PermissionPolicyName).
        foreach (var permission in Enum.GetValues<AdminPermission>())
        {
            if (permission is AdminPermission.None or AdminPermission.All) continue;

            authorizationBuilder.AddPolicy(
                PermissionPolicyName(permission),
                policy => policy.RequireRole(Roles.Admin).RequireClaim(PermissionClaimType, permission.ToString()));
        }

        return services;
    }
}
