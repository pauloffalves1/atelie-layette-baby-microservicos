using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    // Recycling pooled connections is what lets a deploy replace a backend container without
    // restarting this one. Docker gives the recreated container a new IP; connections pooled
    // against the old one keep failing until they are discarded, which is why the deploy used to
    // end with "docker compose restart gateway" — and that restart, not the backend swap, is what
    // dropped the requests in flight (Nginx logged "Connection reset by peer" from a customer
    // browsing /loja on 2026-09-18). With a short lifetime the gateway re-resolves the name and
    // heals on its own, so nothing has to be restarted in front of live traffic.
    .ConfigureHttpClient((_, handler) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(30));

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

    // Routes that call out to the Anthropic API per request — a real per-call cost, unlike the
    // rest of the catalog's read endpoints. Looser than "auth" (this guards spend, not brute force).
    options.AddPolicy("ai-cost", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Request.Path}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
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
