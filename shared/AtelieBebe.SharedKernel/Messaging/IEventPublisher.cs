namespace AtelieBebe.SharedKernel.Messaging;

/// <summary>Publishes a domain event captured in a service's outbox to the shared RabbitMQ exchange.</summary>
public interface IEventPublisher
{
    Task PublishAsync(string eventType, string jsonContent, CancellationToken ct = default);
}
