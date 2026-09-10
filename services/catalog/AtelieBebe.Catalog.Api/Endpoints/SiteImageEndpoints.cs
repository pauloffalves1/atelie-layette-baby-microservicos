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

            var extension = ImageUploadValidator.ValidateAndGetExtension(file);

            var fileName = $"{key}-{Guid.NewGuid():N}{extension}";
            await using var stream = file.OpenReadStream();
            var url = await fileStorage.SaveAsync("site", fileName, stream, ct);

            return Results.Ok(await service.SetImageAsync(key, url, ct));
        }).DisableAntiforgery();
    }
}
