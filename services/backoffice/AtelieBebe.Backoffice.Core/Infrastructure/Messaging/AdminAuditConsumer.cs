using System.Text.Json;
using AtelieBebe.Backoffice.Core.Application.Audit;
using AtelieBebe.SharedKernel.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtelieBebe.Backoffice.Core.Infrastructure.Messaging;

/// <summary>Subscribes to "AdminActionPerformed" (published best-effort by Identity/Catalog/Orders — see AdminAuditPublisher) and persists it as an AuditLog row.</summary>
public sealed class AdminAuditConsumer : RabbitMqEventConsumerBase
{
    public AdminAuditConsumer(IOptions<RabbitMqOptions> options, IServiceScopeFactory scopeFactory, ILogger<AdminAuditConsumer> logger)
        : base(options, scopeFactory, logger, queueName: "backoffice.admin-audit", routingKeys: "AdminActionPerformed")
    {
    }

    protected override async Task HandleAsync(string eventType, string jsonContent, IServiceProvider scopedServices, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<AdminActionPayload>(jsonContent)
            ?? throw new InvalidOperationException("Payload de auditoria inválido.");

        var auditLogService = scopedServices.GetRequiredService<IAuditLogService>();
        await auditLogService.RecordAsync(payload.ActorId, payload.ActorName, payload.Action, payload.Details, ct);
    }

    private sealed record AdminActionPayload(Guid ActorId, string ActorName, string Action, string Details, DateTime OccurredOn);
}
