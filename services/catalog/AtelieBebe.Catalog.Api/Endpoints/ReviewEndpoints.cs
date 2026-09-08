using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Catalog.Api.Common;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.Catalog.Core.Application.Reviews;

namespace AtelieBebe.Catalog.Api.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products/{productId:guid}/reviews").WithTags("Avaliações");

        group.MapGet("/", async (Guid productId, IReviewService service, CancellationToken ct) =>
            Results.Ok(await service.ListByProductAsync(productId, ct)));

        group.MapGet("/eligibility", async (Guid productId, HttpContext http, IReviewService service, CancellationToken ct) =>
            Results.Ok(await service.GetEligibilityAsync(productId, http.User.GetUserId(), ct)))
            .RequireAuthorization("CustomerOnly");

        group.MapPost("/", async (Guid productId, CreateReviewRequest request, HttpContext http, IReviewService service, CancellationToken ct) =>
            Results.Ok(await service.CreateAsync(productId, http.User.GetUserId(), http.User.GetName(), request, ct)))
            .RequireAuthorization("CustomerOnly");

        group.MapPost("/photo", async (IFormFile file, IFileStorageService fileStorage, CancellationToken ct) =>
        {
            var extension = ImageUploadValidator.ValidateAndGetExtension(file);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = file.OpenReadStream();
            var url = await fileStorage.SaveAsync("reviews", fileName, stream, ct);

            return Results.Ok(new { url });
        })
        .RequireAuthorization("CustomerOnly")
        .DisableAntiforgery();
    }
}
