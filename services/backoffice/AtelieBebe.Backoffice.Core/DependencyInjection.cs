using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.Audit;
using AtelieBebe.Backoffice.Core.Application.Contact;
using AtelieBebe.Backoffice.Core.Application.Dashboard;
using AtelieBebe.Backoffice.Core.Application.Newsletter;
using AtelieBebe.Backoffice.Core.Domain.Repositories;
using AtelieBebe.Backoffice.Core.Infrastructure;
using AtelieBebe.Backoffice.Core.Infrastructure.ExternalServices;
using AtelieBebe.Backoffice.Core.Infrastructure.Messaging;
using AtelieBebe.Backoffice.Core.Infrastructure.Persistence;
using AtelieBebe.Backoffice.Core.Infrastructure.Persistence.Repositories;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Backoffice.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddBackofficeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=backoffice.db";
        services.AddDbContext<BackofficeDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>());
        });
        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<BackofficeDbContext>());

        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));

        services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
        services.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IBackofficeUnitOfWork, BackofficeUnitOfWork>();

        services.AddScoped<IAppUrlProvider, AppUrlProvider>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<INewsletterService, NewsletterService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddHttpClient<IOrdersServiceClient, OrdersServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Orders"] ?? "http://orders:8080"));
        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Catalog"] ?? "http://catalog:8080"));
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Identity"] ?? "http://identity:8080"));

        services.AddHostedService<AdminAuditConsumer>();
        // Backoffice also raises its own events (ContactMessageReceivedDomainEvent) — same
        // publisher/outbox pattern as every other service, so it needs the publishing side too,
        // not just the consumer above (which also binds RabbitMqOptions from the same "RabbitMq" section).
        services.AddOutboxPublishing(configuration);

        return services;
    }
}
