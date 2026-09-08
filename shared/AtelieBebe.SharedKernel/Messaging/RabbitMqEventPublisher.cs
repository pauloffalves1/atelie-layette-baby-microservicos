using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AtelieBebe.SharedKernel.Messaging;

/// <summary>
/// Publishes to the shared "atelie.events" topic exchange, routing key = event type name (e.g.
/// "OrderCreatedDomainEvent"). One connection/channel pair reused for the process lifetime — fine
/// at this scale (a handful of services, low message volume), no connection pooling needed.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task PublishAsync(string eventType, string jsonContent, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync(ct);
        var body = Encoding.UTF8.GetBytes(jsonContent);

        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: eventType,
            mandatory: false,
            basicProperties: new BasicProperties { ContentType = "application/json", Persistent = true },
            body: body,
            cancellationToken: ct);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

            return _channel;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}
