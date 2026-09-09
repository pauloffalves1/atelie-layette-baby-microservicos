using System.Threading.RateLimiting;
using AtelieBebe.Identity.Api.Endpoints;
using AtelieBebe.Identity.Core;
using AtelieBebe.Identity.Core.Infrastructure.Persistence;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Web;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddOpenApi();

// Same "5 tentativas por minuto por IP+rota" brute-force guard the monolith had — kept here too
// as defense-in-depth even though the Gateway is meant to be the only externally reachable entry
// point (this service is never exposed outside the docker/k8s network on its own).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Request.Path}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapCustomerAddressEndpoints();
app.MapInternalEndpoints();
app.MapHealthChecks("/health");

await DbInitializer.InitializeAsync(app.Services);

app.Run();
