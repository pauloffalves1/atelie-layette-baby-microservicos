using AtelieBebe.Backoffice.Core.Application.Audit;
using AtelieBebe.SharedKernel.Auth;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/audit-log")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Dashboard))
            .WithTags("Auditoria");

        // from/to are calendar days (yyyy-MM-dd) in Brasília; see AuditLogFilter.
        group.MapGet("/", async (IAuditLogService service, CancellationToken ct, int page = 1, int pageSize = 20,
            string? admin = null, string? action = null, DateOnly? from = null, DateOnly? to = null, string? search = null) =>
            Results.Ok(await service.ListAsync(page, pageSize, AuditLogFilter.FromQuery(admin, action, from, to, search), ct)));

        group.MapGet("/filters", async (IAuditLogService service, CancellationToken ct) =>
            Results.Ok(await service.GetFilterOptionsAsync(ct)));
    }
}
