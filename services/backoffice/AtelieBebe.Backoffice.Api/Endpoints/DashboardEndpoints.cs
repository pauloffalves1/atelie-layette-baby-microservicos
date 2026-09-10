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
    }
}
