using AtelieBebe.Backoffice.Core.Application.Audit;

namespace AtelieBebe.Backoffice.Core.Tests.Application;

public class AuditLogFilterTests
{
    [Fact]
    public void FromQuery_DaysAreBrasiliaCalendarDaysAsUtcHalfOpenRange()
    {
        var filter = AuditLogFilter.FromQuery(null, null, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 14), null);

        Assert.Equal(new DateTime(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc), filter.FromUtc);
        // "até 14/09" runs to midnight of the 15th in Brasília (03:00 UTC), not 21:00 of the 14th.
        Assert.Equal(new DateTime(2026, 9, 15, 3, 0, 0, DateTimeKind.Utc), filter.ToUtcExclusive);
        Assert.Equal(DateTimeKind.Utc, filter.FromUtc!.Value.Kind);
    }

    [Fact]
    public void FromQuery_BlankValuesBecomeNoFilterAndTextIsTrimmed()
    {
        var filter = AuditLogFilter.FromQuery("  ", "", null, null, "  pedido #722 ");

        Assert.Null(filter.AdminName);
        Assert.Null(filter.Action);
        Assert.Null(filter.FromUtc);
        Assert.Null(filter.ToUtcExclusive);
        Assert.Equal("pedido #722", filter.Search);
    }
}
