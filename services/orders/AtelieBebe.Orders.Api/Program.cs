using System.Threading.RateLimiting;
using AtelieBebe.Orders.Api.Endpoints;
using AtelieBebe.Orders.Core;
using AtelieBebe.Orders.Core.Infrastructure.Persistence;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Web;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrdersServices(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddOpenApi();

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

app.MapOrderEndpoints();
app.MapCartSyncEndpoints();
app.MapCouponEndpoints();
app.MapPaymentEndpoints();
app.MapInternalEndpoints();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapFakePaymentEndpoints();
}

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
}

app.Run();
