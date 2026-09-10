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
        await SeedReviewsAsync(dbContext);
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

        // NOTE: this used to also delete any product whose category wasn't in this hardcoded seed
        // list, to enforce a "burp-cloths-only" catalog. That silently destroyed real admin-added
        // products (Almofadinha, Toalha, kits) the moment this container restarted, since the
        // catalog had grown beyond this list without the list being updated. Seeding must only ever
        // add missing demo rows, never delete live data based on a hardcoded allowlist.
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

    // Fixed GUIDs (not Guid.NewGuid()) so re-running this on every restart can check "did I already
    // seed these?" instead of inserting duplicates — same idempotency goal as SeedProductsAsync's
    // slug check, just keyed differently since a review has no natural unique name.
    private static readonly (Guid CustomerId, string CustomerName, string ProductSlug, string Comment)[] FakeReviews =
    [
        (Guid.Parse("9c9b3e1e-1a4e-4a6a-9c1a-1a2b3c4d5e01"), "Marina Souza", "kit-fralda-de-ombro-e-boca-ursinho-bordado",
            "Amei o acabamento e o bordado ficou lindo! Chegou super rápido e a qualidade do tecido é ótima."),
        (Guid.Parse("9c9b3e1e-1a4e-4a6a-9c1a-1a2b3c4d5e02"), "Camila Ferreira", "fralda-de-ombro-bordada-com-nome",
            "Encomendei com o nome da minha filha e ficou perfeito, super caprichado. Recomendo demais!"),
        (Guid.Parse("9c9b3e1e-1a4e-4a6a-9c1a-1a2b3c4d5e03"), "Juliana Alves", "kit-3-fraldas-de-boca-estampadas",
            "Fraldinhas super macias e absorventes, exatamente como nas fotos. Já é a segunda vez que compro."),
    ];

    private static async Task SeedReviewsAsync(CatalogDbContext dbContext)
    {
        var existingCustomerIds = (await dbContext.ProductReviews.Select(r => r.CustomerId).ToListAsync()).ToHashSet();
        var missing = FakeReviews.Where(r => !existingCustomerIds.Contains(r.CustomerId)).ToList();
        if (missing.Count == 0) return;

        var productIdBySlug = await dbContext.Products
            .Where(p => missing.Select(r => r.ProductSlug).Contains(p.Slug))
            .ToDictionaryAsync(p => p.Slug, p => p.Id);

        var newReviews = missing
            .Where(r => productIdBySlug.ContainsKey(r.ProductSlug))
            .Select(r => ProductReview.Create(productIdBySlug[r.ProductSlug], r.CustomerId, r.CustomerName, 5, r.Comment))
            .ToList();

        foreach (var review in newReviews)
            review.Approve();

        if (newReviews.Count == 0) return;

        dbContext.ProductReviews.AddRange(newReviews);
        await dbContext.SaveChangesAsync();
    }
}
