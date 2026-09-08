using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AtelieBebe.SharedKernel.Messaging;

namespace AtelieBebe.SharedKernel.Outbox;

/// <summary>
/// Polls this service's own outbox table and publishes each pending event to RabbitMQ — the
/// microservices equivalent of the monolith's OutboxProcessor, minus the in-process switch/dispatch
/// (that becomes each subscriber's own job). Same batch size, poll interval and retry cap as the
/// monolith, so the at-least-once/crash-safe guarantees carry over unchanged.
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(IServiceScopeFactory scopeFactory, IEventPublisher publisher, ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar lote da outbox.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            try
            {
                await _publisher.PublishAsync(message.Type, message.Content, ct);
                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Attempts += 1;
                message.Error = ex.Message;
                _logger.LogError(ex, "Falha ao publicar mensagem da outbox {MessageId} (tentativa {Attempts}).", message.Id, message.Attempts);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
