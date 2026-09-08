using AtelieBebe.Identity.Core.Application.Customers;

namespace AtelieBebe.Identity.Api.Endpoints;

/// <summary>
/// Service-to-service only — never routed through the Gateway. Currently just backs the
/// Backoffice dashboard's "TotalCustomers" figure (API composition, see Requisito de arquitetura
/// no plano de microsserviços).
/// </summary>
public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this WebApplication app)
    {
        app.MapGet("/internal/customers/count", async (ICustomerAdminService service, CancellationToken ct) =>
            Results.Ok(new { count = (await service.ListAsync(ct)).Count }));

        // The abandoned-cart reminder job (Orders) needs the customer's current name/e-mail and
        // whether the account was anonymized (deleted accounts never get marketing e-mails).
        app.MapGet("/internal/customers/{id:guid}", async (Guid id, ICustomerAdminService service, CancellationToken ct) =>
        {
            var customer = await service.GetByIdAsync(id, ct);
            return Results.Ok(new { customer.Id, customer.Name, customer.Email, customer.IsAnonymized });
        });
    }
}
