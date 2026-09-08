using AtelieBebe.Notifications.Worker.Abstractions;
using AtelieBebe.Notifications.Worker.Consumers;
using AtelieBebe.Notifications.Worker.ExternalServices;
using AtelieBebe.Notifications.Worker.Infrastructure;
using AtelieBebe.SharedKernel.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.Notifications.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<AdminNotificationOptions>(configuration.GetSection(AdminNotificationOptions.SectionName));
        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        services.AddScoped<IAppUrlProvider, AppUrlProvider>();
        services.AddHttpClient<INotificationSender, WhatsAppNotificationSender>(client =>
            client.BaseAddress = new Uri("https://graph.facebook.com/"));
        services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
            client.BaseAddress = new Uri("https://api.resend.com/"));

        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Catalog"] ?? "http://catalog:8080"));
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((sp, client) =>
            client.BaseAddress = new Uri(configuration["Services:Identity"] ?? "http://identity:8080"));

        services.AddHostedService<NotificationsEventConsumer>();

        return services;
    }
}
