namespace AtelieBebe.SharedKernel.Common;

/// <summary>
/// The atelier's local time zone, for anything the server itself turns into calendar days or text
/// (dashboard day buckets, CSV exports). JSON payloads stay UTC — the browser localizes those.
/// </summary>
public static class BrasiliaTime
{
    /// <summary>Brazil has had no daylight saving since 2019, so a fixed UTC-3 is a safe fallback when
    /// the container image has no tz database.</summary>
    public static TimeZoneInfo Zone { get; } = Resolve();

    /// <summary>Converts a UTC instant (any Kind other than Local is treated as UTC) to Brasília wall-clock time.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>Converts a Brasília wall-clock time (e.g. the start of a day picked in the admin) to UTC.</summary>
    public static DateTime ToUtc(DateTime brasilia) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(brasilia, DateTimeKind.Unspecified), Zone);

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        }
        return TimeZoneInfo.CreateCustomTimeZone("Brasilia-fixed", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }
}
