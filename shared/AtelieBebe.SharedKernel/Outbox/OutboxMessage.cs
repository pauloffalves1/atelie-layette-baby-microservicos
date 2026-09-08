namespace AtelieBebe.SharedKernel.Outbox;

/// <summary>
/// Transactional outbox: domain events are serialized into rows in the SAME database
/// transaction as the aggregate change that raised them, guaranteeing at-least-once,
/// crash-safe capture. Each service's own <see cref="Messaging.OutboxPublisher"/> later reads
/// and publishes them to RabbitMQ instead of dispatching in-process (the monolith's approach).
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
    public int Attempts { get; set; }
}
