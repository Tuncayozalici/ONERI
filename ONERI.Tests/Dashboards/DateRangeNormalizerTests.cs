using ONERI.Services.Dashboards;
using Xunit;

namespace ONERI.Tests.Dashboards;

public class DateRangeNormalizerTests
{
    [Fact]
    public void Normalize_SingleDay_KeepsSameStartAndEnd()
    {
        var day = new DateTime(2026, 2, 18);

        var result = DateRangeNormalizer.Normalize(day, day);

        Assert.Equal(day, result.StartDate);
        Assert.Equal(day, result.EndDate);
    }

    [Fact]
    public void Normalize_DateRange_KeepsBoundaries()
    {
        var start = new DateTime(2026, 2, 10);
        var end = new DateTime(2026, 2, 18);

        var result = DateRangeNormalizer.Normalize(start, end);

        Assert.Equal(start, result.StartDate);
        Assert.Equal(end, result.EndDate);
    }

    [Fact]
    public void Normalize_MonthRange_KeepsFirstAndLastDayOfMonth()
    {
        var firstDay = new DateTime(2026, 2, 1);
        var lastDay = new DateTime(2026, 2, 28);

        var result = DateRangeNormalizer.Normalize(firstDay, lastDay);

        Assert.Equal(firstDay, result.StartDate);
        Assert.Equal(lastDay, result.EndDate);
    }
}
