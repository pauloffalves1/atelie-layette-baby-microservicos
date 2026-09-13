using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AtelieBebe.SharedKernel.Common;

/// <summary>
/// Every DateTime this system stores is UTC (server code only ever uses DateTime.UtcNow, and the
/// admin's date inputs are sent as ISO strings with "Z"). SQL Server's datetime2 has no kind though,
/// so EF read them back as <see cref="DateTimeKind.Unspecified"/> — which System.Text.Json writes
/// without an offset, and the browser then parsed a UTC clock time as local: every date on the
/// storefront and admin showed 3h ahead in Brasília. These converters stamp values read from the
/// database as UTC so the API emits "...Z" and the browser converts to local time.
/// </summary>
public static class UtcDateTimeConventions
{
    /// <summary>Call from each service DbContext's <c>ConfigureConventions</c>.</summary>
    public static ModelConfigurationBuilder UseUtcDateTimes(this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        return configurationBuilder;
    }

    /// <summary>A Local-kind value is converted; an Unspecified one is assumed to already be UTC.</summary>
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => ToUtc(value),
        stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc));

    public sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        value => value.HasValue ? ToUtc(value.Value) : value,
        stored => stored.HasValue ? DateTime.SpecifyKind(stored.Value, DateTimeKind.Utc) : stored);
}
