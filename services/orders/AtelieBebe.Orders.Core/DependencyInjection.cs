using AtelieBebe.Orders.Core.Application.Abstractions;
using AtelieBebe.Orders.Core.Application.Cart;
using AtelieBebe.Orders.Core.Application.Coupons;
using AtelieBebe.Orders.Core.Application.Orders;
using AtelieBebe.Orders.Core.Domain.Repositories;
using AtelieBebe.Orders.Core.Infrastructure;
using AtelieBebe.Orders.Core.Infrastructure.Cart;
using AtelieBebe.Orders.Core.Infrastructure.ExternalServices;
using AtelieBebe.Orders.Core.Infrastructure.Payments;
using AtelieBebe.Orders.Core.Infrastructure.Persistence;
using AtelieBebe.Orders.Core.Infrastructure.Persistence.Repositories;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Orders.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
    {
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=orders.db";
        services.AddDbContext<OrdersDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>());
        });
        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<OrdersDbContext>());

        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));
        services.Configure<PagBankOptions>(configuration.GetSection(PagBankOptions.SectionName));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<ICartSnapshotRepository, CartSnapshotRepository>();
        services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();

        services.AddScoped<IAppUrlProvider, AppUrlProvider>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICartSyncService, CartSyncService>();
        services.AddScoped<ICouponService, CouponService>();

        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Catalog"] ?? "http://catalog:8080"));
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Identity"] ?? "http://identity:8080"));

        var pagBankToken = configuration[$"{PagBankOptions.SectionName}:Token"];
        if (isDevelopment && string.IsNullOrWhiteSpace(pagBankToken))
        {
            services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        }
        else
        {
            services.AddHttpClient<IPaymentGateway, PagBankGateway>((sp, client) =>
            {
                var sandbox = sp.GetRequiredService<IOptions<PagBankOptions>>().Value.Sandbox;
                client.BaseAddress = new Uri(sandbox ? "https://sandbox.api.pagseguro.com/" : "https://api.pagseguro.com/");
            });
        }

        services.AddHostedService<AbandonedCartReminderProcessor>();
        services.AddOutboxPublishing(configuration);

        return services;
    }
}
