using AtelieBebe.Catalog.Core.Domain.Entities;
using AtelieBebe.SharedKernel.Common;
using AtelieBebe.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Catalog.Core.Infrastructure.Persistence;

/// <summary>Applies migrations and seeds the demo catalog — same seed data and specialization rule as the monolith's DbInitializer.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await dbContext.Database.MigrateAsync();
        await SeedProductsAsync(dbContext);
    }

    private static async Task SeedProductsAsync(CatalogDbContext dbContext)
    {
        var seedData = new (string Name, string Category, decimal Price, bool Featured, string Description)[]
        {
            // Kit Ombro e Boca
            ("Kit Fralda de Ombro e Boca Ursinho Bordado", "Kit Ombro e Boca", 79.90m, true, "Kit com 1 fralda de ombro e 1 fralda de boca em algodão, bordado de ursinho."),
            ("Kit Fralda de Ombro e Boca Nuvem Bordada", "Kit Ombro e Boca", 79.90m, true, "Kit com 1 fralda de ombro e 1 fralda de boca, bordado de nuvens, tecido macio e absorvente."),
            ("Kit 3 Fraldas de Ombro e Boca Coordenadas", "Kit Ombro e Boca", 119.90m, true, "Kit com 3 conjuntos de fralda de ombro e boca coordenados, estampas exclusivas do ateliê."),
            // Fralda de Ombro (só ombro)
            ("Fralda de Ombro Bordada Golfinho", "Fralda de Ombro", 49.90m, false, "Fralda de ombro avulsa em algodão, bordado de golfinho, tamanho reforçado para proteger a roupa."),
            ("Fralda de Ombro Xadrez Piquet", "Fralda de Ombro", 44.90m, false, "Fralda de ombro em piquet xadrez, absorvente e resistente ao uso diário."),
            ("Fralda de Ombro Bordada com Nome", "Fralda de Ombro", 54.90m, true, "Fralda de ombro avulsa com bordado personalizado do nome do bebê."),
            // Fralda de Boca (só boca)
            ("Fralda de Boca Tricô Bordada", "Fralda de Boca", 29.90m, false, "Fralda de boca avulsa em tricô, bordado delicado, ideal para arrotinhos e babados."),
            ("Kit 3 Fraldas de Boca Estampadas", "Fralda de Boca", 39.90m, true, "Kit com 3 fraldas de boca em algodão estampado, super macias e absorventes."),
            ("Fralda de Boca Bordada Florzinha", "Fralda de Boca", 34.90m, false, "Fralda de boca avulsa com bordado floral."),
        };

        var allowedCategories = seedData.Select(p => p.Category).ToHashSet();
        var discontinued = await dbContext.Products.Where(p => !allowedCategories.Contains(p.Category)).ToListAsync();
        if (discontinued.Count > 0)
        {
            dbContext.Products.RemoveRange(discontinued);
            await dbContext.SaveChangesAsync();
        }

        var existingSlugs = (await dbContext.Products.Select(p => p.Slug).ToListAsync()).ToHashSet();

        var newProducts = seedData
            .Select((p, index) => (Product: p, ImageIndex: index + 1))
            .Where(x => !existingSlugs.Contains(SlugHelper.Slugify(x.Product.Name)))
            .Select(x => Product.Create(
                x.Product.Name,
                SlugHelper.Slugify(x.Product.Name),
                x.Product.Description,
                Money.FromReais(x.Product.Price),
                x.Product.Category,
                $"https://picsum.photos/seed/atelie-bebe-{x.ImageIndex}/600/600",
                x.Product.Featured))
            .ToList();

        if (newProducts.Count == 0) return;

        dbContext.Products.AddRange(newProducts);
        await dbContext.SaveChangesAsync();
    }
}
