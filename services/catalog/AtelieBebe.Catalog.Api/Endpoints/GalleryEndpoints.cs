using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Catalog.Api.Common;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.Catalog.Core.Application.Gallery;

namespace AtelieBebe.Catalog.Api.Endpoints;

public static class GalleryEndpoints
{
    public static void MapGalleryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/gallery-images", async (IGalleryImageService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)))
            .WithTags("Galeria");

        var adminGroup = app.MapGroup("/api/admin/gallery-images").WithTags("Galeria (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.SiteContent));

        adminGroup.MapPost("/", async (IFormFile file, IFileStorageService fileStorage, IGalleryImageService service, CancellationToken ct) =>
        {
            var extension = ImageUploadValidator.ValidateAndGetExtension(file);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = file.OpenReadStream();
            var url = await fileStorage.SaveAsync("gallery", fileName, stream, ct);

            var image = await service.AddAsync(url, ct);
            return Results.Created($"/api/gallery-images/{image.Id}", image);
        }).DisableAntiforgery();

        adminGroup.MapDelete("/{id:guid}", async (Guid id, IGalleryImageService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
