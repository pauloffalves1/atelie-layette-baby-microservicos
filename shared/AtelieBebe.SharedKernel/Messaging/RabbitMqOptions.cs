namespace AtelieBebe.SharedKernel.Messaging;

/// <summary>Bound from the "RabbitMq" config section — same shape in every service's appsettings.json.</summary>
public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Single topic exchange every service publishes to and consumes from. Routing key = event type name.</summary>
    public string Exchange { get; set; } = "atelie.events";
}
