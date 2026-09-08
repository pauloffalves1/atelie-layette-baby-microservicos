using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Single place the browser ever talks to (every backend service only accepts requests from inside
// the docker/k8s network) — so CORS lives here now, not duplicated across four services.
const string CorsPolicyName = "AtelieBebeCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// A coarser, gateway-level version of the same brute-force guard each service also applies to its
// own sensitive endpoints — this one sees the real client IP directly (no X-Forwarded-For chain to
// trust), so it's the one that actually rate-limits per visitor; the per-service policies are a
// second, imprecise (shared "the gateway's IP" bucket) safety net behind it.
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

app.UseCors(CorsPolicyName);
app.UseRateLimiter();

app.MapHealthChecks("/health");

// Which routes get the "auth" rate-limit policy is declared per-route in appsettings
// (ReverseProxy:Routes:*:RateLimiterPolicy) — YARP applies it automatically via LoadFromConfig,
// same "auth" policy name registered above.
app.MapReverseProxy();

app.Run();
