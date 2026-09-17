using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.Backoffice.Core.Application.Audit;

/// <summary>
/// Audit log listing filters. Dates arrive as calendar days as the admin sees them (Brasília) and
/// become a UTC half-open range [FromUtc, ToUtcExclusive) — "até 14/09" includes everything done on
/// the 14th until midnight in Brasília, not until 21:00 (midnight UTC).
/// </summary>
public sealed record AuditLogFilter(string? AdminName, string? Action, DateTime? FromUtc, DateTime? ToUtcExclusive, string? Search)
{
    public static readonly AuditLogFilter None = new(null, null, null, null, null);

    public static AuditLogFilter FromQuery(string? admin, string? action, DateOnly? from, DateOnly? to, string? search) => new(
        Blank(admin),
        Blank(action),
        from is { } start ? BrasiliaTime.ToUtc(start.ToDateTime(TimeOnly.MinValue)) : null,
        to is { } end ? BrasiliaTime.ToUtc(end.AddDays(1).ToDateTime(TimeOnly.MinValue)) : null,
        Blank(search));

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Values that actually occur in the log, for the listing's dropdowns.</summary>
public sealed record AuditLogFilterOptionsDto(IReadOnlyList<string> Admins, IReadOnlyList<string> Actions);
