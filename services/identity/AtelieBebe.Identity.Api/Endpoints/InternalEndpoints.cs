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
        // The accounts used to test the real checkout in production (RF40) are ordinary accounts —
        // they just shouldn't be counted as customers of the ateliê. Configured as
        // Testing:AccountEmails (TESTING__ACCOUNTEMAILS, comma-separated); empty means count everyone.
        app.MapGet("/internal/customers/count", async (ICustomerAdminService service, IConfiguration configuration, CancellationToken ct) =>
        {
            var testEmails = (configuration["Testing:AccountEmails"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var customers = await service.ListAsync(ct);
            var count = testEmails.Count == 0 ? customers.Count : customers.Count(c => !testEmails.Contains(c.Email));

            return Results.Ok(new { count });
        });

        // The abandoned-cart reminder job (Orders) needs the customer's current name/e-mail and
        // whether the account was anonymized (deleted accounts never get marketing e-mails).
        app.MapGet("/internal/customers/{id:guid}", async (Guid id, ICustomerAdminService service, CancellationToken ct) =>
        {
            var customer = await service.GetByIdAsync(id, ct);
            return Results.Ok(new { customer.Id, customer.Name, customer.Email, customer.IsAnonymized });
        });
    }
}
