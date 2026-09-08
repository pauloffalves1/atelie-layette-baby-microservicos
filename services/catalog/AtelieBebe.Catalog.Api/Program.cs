using AtelieBebe.Catalog.Api.Endpoints;
using AtelieBebe.Catalog.Core;
using AtelieBebe.Catalog.Core.Infrastructure.Persistence;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogServices(builder.Configuration);
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

app.MapProductEndpoints();
app.MapReviewEndpoints();
app.MapWishlistEndpoints();
app.MapGalleryEndpoints();
app.MapSiteImageEndpoints();
app.MapInternalEndpoints();
app.MapHealthChecks("/health");

// Served the same way the monolith served /api/uploads/** — a plain static-file mapping over the
// folder LocalFileStorageService writes to.
var uploadsPath = Path.GetFullPath(builder.Configuration["Uploads:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads"));
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = builder.Configuration["Uploads:PublicPath"] ?? "/api/uploads",
});

await DbInitializer.InitializeAsync(app.Services);

app.Run();
