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

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin))
            .AddPolicy("CustomerOnly", policy => policy.RequireRole(Roles.Customer));

        return services;
    }
}
