using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AtelieBebe.SharedKernel.Messaging;

/// <summary>
/// Publishes "AdminActionPerformed" straight to RabbitMQ (via <see cref="IEventPublisher"/>), not
/// through a service's transactional outbox: audit entries aren't part of any aggregate's state
/// change, so there's no natural entity to hang a domain event off of, and losing an occasional
/// audit row on a rare publish failure is an acceptable trade-off this project already doesn't
/// extend the same "at-least-once, crash-safe" guarantee to (unlike real business notifications,
/// which do go through the outbox). Backoffice consumes this routing key and persists AuditLog rows.
/// Failures are swallowed (logged, not rethrown) — an admin action (e.g. logging in) must still
/// succeed even if RabbitMQ is briefly unreachable; audit logging is best-effort by design here.
/// </summary>
public sealed class AdminAuditPublisher
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<AdminAuditPublisher> _logger;

    public AdminAuditPublisher(IEventPublisher publisher, ILogger<AdminAuditPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishAsync(Guid actorId, string actorName, string action, string details, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new
        {
            ActorId = actorId,
            ActorName = actorName,
            Action = action,
            Details = details,
            OccurredOn = DateTime.UtcNow,
        });

        try
        {
            await _publisher.PublishAsync("AdminActionPerformed", json, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar evento de auditoria {Action} (best-effort, não bloqueia a ação original).", action);
        }
    }
}
