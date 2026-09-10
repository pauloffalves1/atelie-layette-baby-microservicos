using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Catalog.Api.Common;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.Catalog.Core.Application.Reviews;

namespace AtelieBebe.Catalog.Api.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        // Under /api/products so it rides the Gateway's existing catalog-products-route catch-all
        // instead of needing its own route entry (bare /api/reviews/... would 404 through the Gateway).
        app.MapGet("/api/products/reviews/featured", async (IReviewService service, CancellationToken ct, int limit = 6) =>
            Results.Ok(await service.ListFeaturedAsync(limit, ct)))
            .WithTags("Avaliações");

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

        var adminGroup = app.MapGroup("/api/admin/reviews").WithTags("Avaliações (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Reviews));

        adminGroup.MapGet("/", async (bool? approved, IReviewService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.ListForAdminAsync(approved, page, pageSize, ct)));

        adminGroup.MapPatch("/{id:guid}/approve", async (Guid id, HttpContext http, IReviewService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var review = await service.ApproveAsync(id, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ReviewApproved", $"Avaliação de '{review.CustomerName}' em '{review.ProductName}' aprovada", ct);
            return Results.Ok(review);
        });

        adminGroup.MapDelete("/{id:guid}", async (Guid id, HttpContext http, IReviewService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            await service.RejectAsync(id, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ReviewRejected", "Avaliação rejeitada e removida", ct);
            return Results.NoContent();
        });
    }
}
