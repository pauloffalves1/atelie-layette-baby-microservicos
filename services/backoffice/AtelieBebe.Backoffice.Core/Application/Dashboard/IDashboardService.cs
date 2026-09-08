namespace AtelieBebe.Backoffice.Core.Application.Dashboard;

/// <summary>
/// Read-only reporting query, intentionally outside the Repository/UnitOfWork write model —
/// implemented against the persistence store directly for efficient aggregation (CQRS-style read side).
/// </summary>
public interface IDashboardService
{
    Task<DashboardDto> GetSummaryAsync(CancellationToken ct = default);
}
