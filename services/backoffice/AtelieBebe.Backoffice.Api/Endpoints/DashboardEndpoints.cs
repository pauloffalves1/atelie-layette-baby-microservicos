using AtelieBebe.Backoffice.Core.Application.Abstractions;
using AtelieBebe.Backoffice.Core.Application.Dashboard;
using AtelieBebe.SharedKernel.Auth;

namespace AtelieBebe.Backoffice.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/dashboard", async (IDashboardService service, CancellationToken ct) =>
            Results.Ok(await service.GetSummaryAsync(ct)))
            .WithTags("Dashboard (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Dashboard));

        // Takes the DashboardDto the admin already has loaded (from GET above) instead of
        // recomputing it — avoids a second fan-out to Orders/Catalog/Identity for an on-demand action.
        app.MapPost("/api/admin/dashboard/summary", async (DashboardDto dashboard, IDashboardSummaryGenerator generator, CancellationToken ct) =>
            Results.Ok(new { summary = await generator.SummarizeAsync(dashboard, ct) }))
            .WithTags("Dashboard (admin)")
            .RequireAuthorization(JwtAuthenticationExtensions.PermissionPolicyName(AdminPermission.Dashboard));
    }
}
