using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Backoffice's read-only dependency on Orders — everything in the dashboard except product/customer counts.</summary>
public interface IOrdersServiceClient
{
    Task<OrdersDashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default);
}
