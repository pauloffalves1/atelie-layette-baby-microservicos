using AtelieBebe.SharedKernel.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AtelieBebe.SharedKernel.Messaging;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RabbitMQ publisher and the outbox-polling background service that drains this
    /// service's own outbox table into it. Call from every service that raises domain events
    /// (Identity, Catalog, Orders, Backoffice) — not from Notifications, which only consumes.
    /// </summary>
    public static IServiceCollection AddOutboxPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<AdminAuditPublisher>();
        services.AddHostedService<OutboxPublisherService>();
        return services;
    }
}
