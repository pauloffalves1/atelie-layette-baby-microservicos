using AtelieBebe.Identity.Core.Application.Admins;
using AtelieBebe.SharedKernel.Auth;
using AtelieBebe.SharedKernel.Messaging;
using AtelieBebe.SharedKernel.Web;

namespace AtelieBebe.Identity.Api.Endpoints;

/// <summary>
/// Only an admin holding <see cref="AdminPermission.AdminManagement"/> can register other admins or
/// change anyone's permission set — see <see cref="Core.Application.Admins.AdminManagementService"/>
/// for the "can't remove the last admin who can manage admins" guard.
/// </summary>
public static class AdminManagementEndpoints
{
    public static void MapAdminManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/admins").WithTags("Administradores (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.AdminManagement));

        group.MapGet("/", async (IAdminManagementService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        group.MapPost("/", async (CreateAdminRequest request, HttpContext http, IAdminManagementService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "AdminCreated", $"Administrador '{created.Name}' ({created.Email}) cadastrado", ct);
            return Results.Ok(created);
        });

        group.MapPut("/{id:guid}/permissions", async (Guid id, UpdateAdminPermissionsRequest request, HttpContext http, IAdminManagementService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var updated = await service.UpdatePermissionsAsync(id, request, ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "AdminPermissionsUpdated", $"Permissões de '{updated.Name}' atualizadas: {string.Join(", ", updated.Permissions)}", ct);
            return Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext http, IAdminManagementService service, AdminAuditPublisher auditPublisher, CancellationToken ct) =>
        {
            var removed = await service.RemoveAsync(id, http.User.GetUserId(), ct);
            await auditPublisher.PublishAsync(http.User.GetUserId(), http.User.GetName(), "AdminRemoved", $"Administrador '{removed.Name}' ({removed.Email}) removido", ct);
            return Results.NoContent();
        });
    }
}
