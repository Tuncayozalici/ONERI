namespace ONERI.Services.Dashboards;

public static class DateRangeNormalizer
{
    public static (DateTime StartDate, DateTime EndDate) Normalize(DateTime startDate, DateTime endDate)
    {
        var normalizedStart = startDate.Date;
        var normalizedEnd = endDate.Date;

        if (normalizedEnd < normalizedStart)
        {
            (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);
        }

        return (normalizedStart, normalizedEnd);
    }
}
