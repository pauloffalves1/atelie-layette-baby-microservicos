using AtelieBebe.Backoffice.Core.Application.Audit;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/audit-log", async (IAuditLogService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.ListAsync(page, pageSize, ct)))
            .RequireAuthorization("AdminOnly")
            .WithTags("Auditoria");
    }
}
