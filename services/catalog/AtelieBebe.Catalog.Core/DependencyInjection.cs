using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Application.Gallery;
using AtelieBebe.Catalog.Core.Application.Products;
using AtelieBebe.Catalog.Core.Application.Reviews;
using AtelieBebe.Catalog.Core.Application.SiteImages;
using AtelieBebe.Catalog.Core.Application.Wishlist;
using AtelieBebe.Catalog.Core.Domain.Repositories;
using AtelieBebe.Catalog.Core.Infrastructure.ExternalServices;
using AtelieBebe.Catalog.Core.Infrastructure.Persistence;
using AtelieBebe.Catalog.Core.Infrastructure.Persistence.Repositories;
using AtelieBebe.Catalog.Core.Infrastructure.Storage;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Catalog.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=catalog.db";
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>());
        });
        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
        services.AddScoped<IWishlistItemRepository, WishlistItemRepository>();
        services.AddScoped<IGalleryImageRepository, GalleryImageRepository>();
        services.AddScoped<ISiteImageRepository, SiteImageRepository>();
        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IGalleryImageService, GalleryImageService>();
        services.AddScoped<ISiteImageService, SiteImageService>();

        services.AddHttpClient<IOrdersServiceClient, OrdersServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Orders"] ?? "http://orders:8080"));

        services.AddOutboxPublishing(configuration);

        return services;
    }
}
