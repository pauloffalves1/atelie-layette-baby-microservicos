using AtelieBebe.Backoffice.Api.Endpoints;
using AtelieBebe.Backoffice.Core;
using AtelieBebe.Backoffice.Core.Infrastructure.Persistence;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBackofficeServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapContactEndpoints();
app.MapNewsletterEndpoints();
app.MapAuditLogEndpoints();
app.MapDashboardEndpoints();
app.MapSitemapEndpoints();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BackofficeDbContext>().Database.MigrateAsync();
}

app.Run();
