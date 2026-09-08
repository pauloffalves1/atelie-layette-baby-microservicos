using System.Text.Json;
using AtelieBebe.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AtelieBebe.SharedKernel.Outbox;

/// <summary>
/// Before every SaveChanges, scans tracked aggregate roots for domain events raised during the
/// use case and appends one OutboxMessage row per event to the same change set — so the aggregate
/// change and its outbox entries commit or roll back together as a single atomic transaction.
/// Shared by every microservice's DbContext (each has its own outbox table).
/// </summary>
public sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDomainEventsToOutboxMessages(DbContext? context)
    {
        if (context is null) return;

        var entitiesWithEvents = context.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        // Short type name only (not AssemblyQualifiedName): the routing key/consumer contract is
        // the event name + its JSON shape, not a CLR type — the publishing and consuming services
        // are separate assemblies/processes and don't share the event's actual .NET type.
        var outboxMessages = entitiesWithEvents
            .SelectMany(entity => entity.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.EventId,
                Type = domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                OccurredOn = domainEvent.OccurredOn,
                ProcessedOn = null,
                Attempts = 0,
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();
    }
}
