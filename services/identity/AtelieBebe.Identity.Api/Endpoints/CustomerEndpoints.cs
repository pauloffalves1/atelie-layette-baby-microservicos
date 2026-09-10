using AtelieBebe.Identity.Core.Application.Customers;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.SharedKernel.Web;

namespace AtelieBebe.Identity.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var adminGroup = app.MapGroup("/api/admin/customers").WithTags("Clientes (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Customers));

        adminGroup.MapGet("/", async (ICustomerAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        adminGroup.MapGet("/{id:guid}", async (Guid id, ICustomerAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(id, ct)));

        adminGroup.MapPut("/{id:guid}", async (Guid id, UpdateCustomerRequest request, HttpContext http, ICustomerAdminService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            var updated = await service.UpdateAsync(id, request, ct);
            var diff = AuditDiff.Join(
                AuditDiff.Field("Nome", before.Name, updated.Name),
                AuditDiff.Field("E-mail", before.Email, updated.Email),
                AuditDiff.Field("CPF", before.Cpf, updated.Cpf),
                AuditDiff.Field("Telefone", before.Phone, updated.Phone));
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "CustomerUpdated", $"Cliente '{updated.Name}' — {diff}", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapPost("/{id:guid}/verify-email", async (Guid id, HttpContext http, ICustomerAdminService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var updated = await service.VerifyEmailAsync(id, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "CustomerEmailVerified", $"E-mail de '{updated.Name}' verificado manualmente pelo admin", ct);
            return Results.Ok(updated);
        });

        adminGroup.MapDelete("/{id:guid}", async (Guid id, HttpContext http, ICustomerAdminService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var before = await service.GetByIdAsync(id, ct);
            await service.RemoveAsync(id, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "CustomerRemoved", $"Cliente '{before.Name}' removido pelo admin", ct);
            return Results.NoContent();
        });
    }
}
