using AtelieBebe.Backoffice.Core.Application.Dashboard;

namespace AtelieBebe.Backoffice.Core.Application.Abstractions;

/// <summary>Turns the dashboard's numbers into a short narrative summary for an admin without time to read charts.</summary>
public interface IDashboardSummaryGenerator
{
    Task<string> SummarizeAsync(DashboardDto dashboard, CancellationToken ct = default);
}
