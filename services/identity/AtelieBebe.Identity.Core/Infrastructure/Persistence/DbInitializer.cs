using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence;

/// <summary>Applies migrations and seeds the admin user — same defaults as the monolith's DbInitializer, minus product seeding (Catalog's job now).</summary>
public static class DbInitializer
{
    // Local-development convenience only: outside Development there is no default password.
    private const string DevelopmentAdminPassword = "admin123";

    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool isDevelopment = false)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await dbContext.Database.MigrateAsync();
        await SeedAdminAsync(dbContext, passwordHasher, configuration, isDevelopment);
    }

    private static async Task SeedAdminAsync(IdentityDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration, bool isDevelopment)
    {
        var email = configuration["AdminSeed:Email"] ?? "admin@ateliebebe.com.br";

        var normalizedEmail = Email.Create(email);
        var exists = await dbContext.Admins.AnyAsync(a => a.Email == normalizedEmail);
        if (exists) return;

        // The repo is public, so a fallback password is a known credential: outside Development a missing
        // AdminSeed:Password must stop the first boot instead of silently seeding a guessable admin.
        // Once the admin exists the password is never read again, so this can't break a running deploy.
        var password = configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            if (!isDevelopment)
                throw new InvalidOperationException(
                    "AdminSeed:Password is not configured. Set ADMIN_SEED_PASSWORD in .env before the first start.");
            password = DevelopmentAdminPassword;
        }

        // The seeded admin is the only one that exists at first boot, so it must hold every
        // permission — otherwise nobody could grant AdminManagement to create the second admin.
        var admin = Admin.Create("Administradora do Ateliê", normalizedEmail, passwordHasher.Hash(password), AdminPermission.All);
        dbContext.Admins.Add(admin);
        await dbContext.SaveChangesAsync();
    }
}
