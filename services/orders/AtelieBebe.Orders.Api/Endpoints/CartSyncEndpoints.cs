using AtelieBebe.SharedKernel.Web;
using AtelieBebe.Orders.Core.Application.Cart;

namespace AtelieBebe.Orders.Api.Endpoints;

public static class CartSyncEndpoints
{
    public static void MapCartSyncEndpoints(this WebApplication app)
    {
        app.MapPut("/api/cart-sync", async (CartSyncRequest request, HttpContext http, ICartSyncService service, CancellationToken ct) =>
        {
            await service.SaveAsync(http.User.GetUserId(), request, ct);
            return Results.NoContent();
        })
        .RequireAuthorization("CustomerOnly")
        .WithTags("Carrinho (sincronização)");
    }
}
