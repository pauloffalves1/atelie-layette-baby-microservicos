using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Catalog.Api.Common;
using AtelieBebe.Catalog.Core.Application.Abstractions;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.Catalog.Core.Application.Products;

namespace AtelieBebe.Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Produtos");

        // Anonymous-friendly: no RequireAuthorization, but an authenticated customer's token
        // (when present) is used to also surface exclusive products they were granted access to.
        group.MapGet("/", async (string? category, string? search, HttpContext http, IProductService service, CancellationToken ct, int page = 1, int pageSize = 12) =>
            Results.Ok(await service.ListAsync(category, onlyActive: true, page, pageSize, http.User.GetUserIdOrNull(), search, ct)));

        group.MapGet("/featured", async (HttpContext http, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.ListFeaturedAsync(http.User.GetUserIdOrNull(), ct)));

        group.MapGet("/categories", async (HttpContext http, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.ListCategoriesAsync(http.User.GetUserIdOrNull(), ct)));

        group.MapGet("/{slug}", async (string slug, HttpContext http, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.GetBySlugAsync(slug, http.User.GetUserIdOrNull(), ct)));

        var adminGroup = app.MapGroup("/api/admin/products").WithTags("Produtos (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Products));

        adminGroup.MapGet("/", async (IProductService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.ListAsync(null, onlyActive: false, page, pageSize, ct: ct)));

        adminGroup.MapGet("/{id:guid}", async (Guid id, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.GetForAdminAsync(id, ct)));

        adminGroup.MapPost("/", async (CreateProductRequest request, HttpContext http, IProductService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ProductCreated", $"Produto '{created.Name}' criado", ct);
            return Results.Created($"/api/admin/products/{created.Id}", created);
        });

        adminGroup.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, HttpContext http, IProductService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            var updated = await service.UpdateAsync(id, request, ct);
            var diff = AuditDiff.Join(
                AuditDiff.Field("Nome", before.Name, updated.Name),
                AuditDiff.Field("Descrição", before.Description, updated.Description),
                AuditDiff.Field("Preço", before.Price, updated.Price),
                AuditDiff.Field("Categoria", before.Category, updated.Category),
                AuditDiff.Field("Destaque", before.Featured, updated.Featured));
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ProductUpdated", $"Produto '{updated.Name}' — {diff}", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapPatch("/{id:guid}/active", async (Guid id, bool active, HttpContext http, IProductService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            var updated = await service.SetActiveAsync(id, active, ct);
            var diff = AuditDiff.Field("Status", before.Active ? "ativo" : "inativo", active ? "ativo" : "inativo") ?? "sem alterações";
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ProductActiveChanged", $"Produto '{updated.Name}' — {diff}", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapDelete("/{id:guid}", async (Guid id, HttpContext http, IProductService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var product = await service.GetByIdAsync(id, ct);
            await service.DeleteAsync(id, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ProductDeleted", $"Produto '{product.Name}' excluído", ct);
            return Results.NoContent();
        });

        adminGroup.MapPut("/{id:guid}/customers", async (Guid id, SetAllowedCustomersRequest request, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.SetAllowedCustomersAsync(id, request, ct)));

        adminGroup.MapPut("/{id:guid}/images", async (Guid id, SetProductImagesRequest request, IProductService service, CancellationToken ct) =>
            Results.Ok(await service.SetImagesAsync(id, request, ct)));

        adminGroup.MapPatch("/{id:guid}/promotion", async (Guid id, SetPromotionRequest request, HttpContext http, IProductService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            var updated = await service.SetPromotionAsync(id, request, ct);
            var oldPromo = before.DiscountPercentage is { } bd ? $"{bd}%" : "sem promoção";
            var newPromo = request.DiscountPercentage is { } nd ? $"{nd}%" : "sem promoção";
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ProductPromotionChanged", $"Produto '{updated.Name}': {oldPromo} → {newPromo}", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapPost("/promotions/bulk", async (BulkApplyPromotionRequest request, HttpContext http, IProductService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var updated = await service.ApplyPromotionToManyAsync(request, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "ProductPromotionChanged", $"Promoção de {request.DiscountPercentage}% aplicada em massa a {updated.Count} produto(s)", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapPost("/uploads", async (IFormFile file, IFileStorageService fileStorage, CancellationToken ct) =>
        {
            var extension = ImageUploadValidator.ValidateAndGetExtension(file);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = file.OpenReadStream();
            var url = await fileStorage.SaveAsync("products", fileName, stream, ct);

            return Results.Ok(new { url });
        }).DisableAntiforgery();
    }
}
