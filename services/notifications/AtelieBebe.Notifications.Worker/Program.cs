using AtelieBebe.Notifications.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotificationsServices(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

// No public API besides a liveness/readiness probe — this service only consumes RabbitMQ events
// and calls out to Catalog/Identity; nothing here is ever routed through the Gateway.
app.MapHealthChecks("/health");

app.Run();
