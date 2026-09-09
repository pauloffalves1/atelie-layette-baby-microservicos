using AtelieBebe.Identity.Core.Application.Addresses;
using AtelieBebe.SharedKernel.Web;

namespace AtelieBebe.Identity.Api.Endpoints;

public static class CustomerAddressEndpoints
{
    public static void MapCustomerAddressEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers/me/addresses").WithTags("Endereços do cliente").RequireAuthorization("CustomerOnly");

        group.MapGet("/", async (HttpContext http, ICustomerAddressService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(http.User.GetUserId(), ct)));

        group.MapPost("/", async (SaveCustomerAddressRequest request, HttpContext http, ICustomerAddressService service, CancellationToken ct) =>
            Results.Ok(await service.CreateAsync(http.User.GetUserId(), request, ct)));

        group.MapPut("/{id:guid}", async (Guid id, SaveCustomerAddressRequest request, HttpContext http, ICustomerAddressService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(http.User.GetUserId(), id, request, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext http, ICustomerAddressService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(http.User.GetUserId(), id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/default", async (Guid id, HttpContext http, ICustomerAddressService service, CancellationToken ct) =>
            Results.Ok(await service.SetDefaultAsync(http.User.GetUserId(), id, ct)));
    }
}
