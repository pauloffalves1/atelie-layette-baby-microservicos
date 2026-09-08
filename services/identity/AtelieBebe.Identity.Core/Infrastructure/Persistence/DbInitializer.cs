using AtelieBebe.Identity.Core.Application.Abstractions;
using AtelieBebe.Identity.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Identity.Core.Infrastructure.Persistence;

/// <summary>Applies migrations and seeds the admin user — same defaults as the monolith's DbInitializer, minus product seeding (Catalog's job now).</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await dbContext.Database.MigrateAsync();
        await SeedAdminAsync(dbContext, passwordHasher, configuration);
    }

    private static async Task SeedAdminAsync(IdentityDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        var email = configuration["AdminSeed:Email"] ?? "admin@ateliebebe.com.br";
        var password = configuration["AdminSeed:Password"] ?? "admin123";

        var normalizedEmail = Email.Create(email);
        var exists = await dbContext.Admins.AnyAsync(a => a.Email == normalizedEmail);
        if (exists) return;

        var admin = Admin.Create("Administradora do Ateliê", normalizedEmail, passwordHasher.Hash(password));
        dbContext.Admins.Add(admin);
        await dbContext.SaveChangesAsync();
    }
}
