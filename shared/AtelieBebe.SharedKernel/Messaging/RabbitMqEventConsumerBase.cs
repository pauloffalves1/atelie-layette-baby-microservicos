using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AtelieBebe.SharedKernel.Messaging;

/// <summary>
/// Base class for a service's background consumer: binds a dedicated, durable queue to the shared
/// "atelie.events" exchange for a fixed set of routing keys (event type names) and hands each
/// message's raw JSON to <see cref="HandleAsync"/>. A failed handler nacks with requeue=false —
/// this project has no dead-letter exchange yet, so a message that keeps failing is dropped rather
/// than looping forever; acceptable for a learning exercise, called out as a known gap.
/// </summary>
public abstract class RabbitMqEventConsumerBase : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly string _queueName;
    private readonly string[] _routingKeys;

    protected RabbitMqEventConsumerBase(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string queueName,
        params string[] routingKeys)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueName = queueName;
        _routingKeys = routingKeys;
    }

    /// <summary>Handle one event's JSON payload. Throw to nack the message (see class remarks).</summary>
    protected abstract Task HandleAsync(string eventType, string jsonContent, IServiceProvider scopedServices, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
        };

        // RabbitMQ isn't guaranteed to be up yet when this service starts (docker-compose/k8s give
        // no ordering guarantee), and a connection failure here must never crash the whole host —
        // this consumer is a side channel, not something the service's own HTTP endpoints depend on.
        // Retry with a fixed backoff instead of letting BackgroundServiceExceptionBehavior take the
        // process down.
        IConnection? connection = null;
        while (connection is null && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Não foi possível conectar ao RabbitMQ para a fila {Queue}; tentando novamente em 5s.", _queueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        if (stoppingToken.IsCancellationRequested) return;

        using var __ = connection;
        using var channel = await connection!.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        foreach (var routingKey in _routingKeys)
            await channel.QueueBindAsync(_queueName, _options.Exchange, routingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var eventType = ea.RoutingKey;
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            using var scope = _scopeFactory.CreateScope();
            try
            {
                await HandleAsync(eventType, json, scope.ServiceProvider, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar evento {EventType} na fila {Queue}.", eventType, _queueName);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(_queueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }
}
