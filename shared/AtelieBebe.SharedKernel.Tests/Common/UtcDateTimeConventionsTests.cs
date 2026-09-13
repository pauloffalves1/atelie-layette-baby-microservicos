using System.Text.Json;
using AtelieBebe.SharedKernel.Common;

namespace AtelieBebe.SharedKernel.Tests.Common;

/// <summary>Dates were shown 3h ahead everywhere because EF read datetime2 back as Kind=Unspecified,
/// which serializes without an offset. These pin the converter and the resulting JSON.</summary>
public class UtcDateTimeConventionsTests
{
    private static readonly UtcDateTimeConventions.UtcDateTimeConverter Converter = new();
    private static readonly UtcDateTimeConventions.NullableUtcDateTimeConverter NullableConverter = new();

    [Fact]
    public void ReadingFromTheDatabase_StampsUtcSoJsonEmitsZ()
    {
        var stored = new DateTime(2026, 9, 13, 14, 41, 28, DateTimeKind.Unspecified);

        var read = (DateTime)Converter.ConvertFromProvider(stored)!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(stored.Ticks, read.Ticks); // same instant, no shifting of the stored value
        Assert.Equal("\"2026-09-13T14:41:28Z\"", JsonSerializer.Serialize(read));
    }

    [Fact]
    public void WritingToTheDatabase_KeepsUtcAndConvertsLocal()
    {
        var utc = new DateTime(2026, 9, 13, 14, 0, 0, DateTimeKind.Utc);
        var local = utc.ToLocalTime();

        Assert.Equal(utc, (DateTime)Converter.ConvertToProvider(utc)!);
        Assert.Equal(utc.Ticks, ((DateTime)Converter.ConvertToProvider(local)!).Ticks);
    }

    [Fact]
    public void NullableConverter_PassesNullThroughAndStampsValues()
    {
        Assert.Null(NullableConverter.ConvertFromProvider(null));

        var read = (DateTime?)NullableConverter.ConvertFromProvider((DateTime?)new DateTime(2026, 9, 13, 10, 0, 0));

        Assert.Equal(DateTimeKind.Utc, read!.Value.Kind);
    }

    [Fact]
    public void BrasiliaTime_ConvertsUtcToLocalWallClock()
    {
        // 01:30 UTC on the 14th is still 22:30 on the 13th in Brasília.
        var local = BrasiliaTime.FromUtc(new DateTime(2026, 9, 14, 1, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 9, 13, 22, 30, 0), local);
    }
}
