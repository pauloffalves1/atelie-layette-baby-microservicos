using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Catalog.Api.Common;
using AtelieBebe.Catalog.Core.Application.Wishlist;

namespace AtelieBebe.Catalog.Api.Endpoints;

public static class WishlistEndpoints
{
    public static void MapWishlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/wishlist").WithTags("Favoritos").RequireAuthorization("CustomerOnly");

        group.MapGet("/", async (HttpContext http, IWishlistService service, CancellationToken ct) =>
            Results.Ok(await service.ListByCustomerAsync(http.User.GetUserId(), ct)));

        group.MapGet("/{productId:guid}", async (Guid productId, HttpContext http, IWishlistService service, CancellationToken ct) =>
            Results.Ok(await service.GetStatusAsync(http.User.GetUserId(), productId, ct)));

        group.MapPost("/{productId:guid}", async (Guid productId, HttpContext http, IWishlistService service, CancellationToken ct) =>
        {
            await service.AddAsync(http.User.GetUserId(), productId, ct);
            return Results.NoContent();
        });

        group.MapDelete("/{productId:guid}", async (Guid productId, HttpContext http, IWishlistService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(http.User.GetUserId(), productId, ct);
            return Results.NoContent();
        });
    }
}
