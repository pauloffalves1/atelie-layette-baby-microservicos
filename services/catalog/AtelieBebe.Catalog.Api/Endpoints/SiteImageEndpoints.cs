using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Catalog.Api.Common;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Exceptions;
using AtelieBebe.Catalog.Core.Application.SiteImages;

namespace AtelieBebe.Catalog.Api.Endpoints;

public static class SiteImageEndpoints
{
    /// <summary>Known image slots the admin can replace. Add new site-image spots here as needed.</summary>
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "home-hero",
        "about",
    };

    public static void MapSiteImageEndpoints(this WebApplication app)
    {
        app.MapGet("/api/site-images", async (ISiteImageService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)))
            .WithTags("Imagens do site");

        var adminGroup = app.MapGroup("/api/admin/site-images").WithTags("Imagens do site (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.SiteContent));

        adminGroup.MapPost("/{key}", async (string key, IFormFile file, IFileStorageService fileStorage,
            ISiteImageService service, CancellationToken ct) =>
        {
            if (!AllowedKeys.Contains(key))
                throw new ConflictException($"Chave de imagem inválida: '{key}'.");

            var url = await SaveUploadAsync(key, file, fileStorage, ct);

            return Results.Ok(await service.SetImageAsync(key, url, ct));
        }).DisableAntiforgery();

        // Multi-image slots (e.g. "home-hero"): appends instead of replacing, so the admin can
        // build up a carousel one photo at a time.
        adminGroup.MapPost("/{key}/items", async (string key, IFormFile file, IFileStorageService fileStorage,
            ISiteImageService service, CancellationToken ct) =>
        {
            if (!AllowedKeys.Contains(key))
                throw new ConflictException($"Chave de imagem inválida: '{key}'.");

            var url = await SaveUploadAsync(key, file, fileStorage, ct);

            return Results.Ok(await service.AddImageAsync(key, url, ct));
        }).DisableAntiforgery();

        adminGroup.MapDelete("/items/{id:guid}", async (Guid id, ISiteImageService service, CancellationToken ct) =>
        {
            await service.DeleteImageAsync(id, ct);
            return Results.NoContent();
        });

        adminGroup.MapPost("/items/{id:guid}/move", async (Guid id, MoveImageRequest request, ISiteImageService service, CancellationToken ct) =>
        {
            if (!Enum.TryParse<MoveDirection>(request.Direction, ignoreCase: true, out var direction))
                throw new ConflictException($"Direção inválida: '{request.Direction}'.");

            await service.MoveImageAsync(id, direction, ct);
            return Results.NoContent();
        });
    }

    private static async Task<string> SaveUploadAsync(string key, IFormFile file, IFileStorageService fileStorage, CancellationToken ct)
    {
        var extension = ImageUploadValidator.ValidateAndGetExtension(file);
        var fileName = $"{key}-{Guid.NewGuid():N}{extension}";
        await using var stream = file.OpenReadStream();
        return await fileStorage.SaveAsync("site", fileName, stream, ct);
    }
}

public sealed record MoveImageRequest(string Direction);
