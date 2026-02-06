using OfficeOpenXml;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ONERI.Services.Dashboards;

public static class DashboardParsingHelper
{
    public static int FindColumn(ExcelWorksheet worksheet, params string[] headers)
    {
        if (worksheet.Dimension == null || headers.Length == 0)
        {
            return -1;
        }

        var normalizedTargets = new HashSet<string>(headers.Select(NormalizeHeaderForMatch));
        int headerRow = 1;
        int maxCol = worksheet.Dimension.Columns;

        for (int col = 1; col <= maxCol; col++)
        {
            var cellValue = worksheet.Cells[headerRow, col].Value?.ToString();
            if (string.IsNullOrWhiteSpace(cellValue))
            {
                continue;
            }

            var normalized = NormalizeHeaderForMatch(cellValue);
            if (normalizedTargets.Contains(normalized))
            {
                return col;
            }
        }

        return -1;
    }

    public static string NormalizeHeaderForMatch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim().ToLowerInvariant();
        text = text.Replace("ı", "i")
                   .Replace("ş", "s")
                   .Replace("ğ", "g")
                   .Replace("ü", "u")
                   .Replace("ö", "o")
                   .Replace("ç", "c")
                   .Replace("İ", "i")
                   .Replace("Ş", "s")
                   .Replace("Ğ", "g")
                   .Replace("Ü", "u")
                   .Replace("Ö", "o")
                   .Replace("Ç", "c");

        var chars = text.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars);
    }

    public static DateTime ParseTurkishDate(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return DateTime.MinValue;
        }

        var trimmed = dateString.Trim();
        var trCulture = new CultureInfo("tr-TR");

        if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out double oaDate))
        {
            if (oaDate >= -657435 && oaDate <= 2958465)
            {
                try
                {
                    return DateTime.FromOADate(oaDate).Date;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
        }

        if (DateTime.TryParse(trimmed, trCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate))
        {
            return parsedDate.Date;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDate))
        {
            return parsedDate.Date;
        }

        if (trimmed.Length == 8 && long.TryParse(trimmed, out _)
            && DateTime.TryParseExact(trimmed, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return parsedDate.Date;
        }

        var monthMap = new Dictionary<string, int>
        {
            { "ocak", 1 }, { "şubat", 2 }, { "mart", 3 }, { "nisan", 4 },
            { "mayıs", 5 }, { "haziran", 6 }, { "temmuz", 7 }, { "ağustos", 8 },
            { "eylül", 9 }, { "ekim", 10 }, { "kasım", 11 }, { "aralık", 12 },
            { "subat", 2 }, { "mayis", 5 }, { "agustos", 8 }, { "eylul", 9 }, { "kasim", 11 }
        };

        var dayNames = new[] { "pazartesi", "salı", "çarşamba", "perşembe", "cuma", "cumartesi", "pazar" };

        var cleanString = trimmed.ToLower(trCulture)
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("/", " ")
            .Replace("-", " ");

        foreach (var dayName in dayNames)
        {
            cleanString = cleanString.Replace(dayName, string.Empty);
        }

        var parts = cleanString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return DateTime.MinValue;
        }

        if (!int.TryParse(parts[0], out var day))
        {
            return DateTime.MinValue;
        }

        var monthToken = parts[1];
        if (!monthMap.TryGetValue(monthToken, out var month))
        {
            return DateTime.MinValue;
        }

        var year = DateTime.Today.Year;
        for (int i = 2; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out var parsedYear))
            {
                year = parsedYear < 100 ? parsedYear + 2000 : parsedYear;
                break;
            }
        }

        try
        {
            return new DateTime(year, month, day);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public static DateTime ParseDateCell(object? value, string? text = null)
    {
        if (value == null)
        {
            return DateTime.MinValue;
        }

        if (value is DateTime dt)
        {
            return dt.Date;
        }

        if (value is double d)
        {
            try
            {
                return DateTime.FromOADate(d).Date;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        var valueText = value.ToString();
        if (!string.IsNullOrWhiteSpace(valueText) && valueText.TrimStart().StartsWith("=") && !string.IsNullOrWhiteSpace(text))
        {
            return ParseTurkishDate(text);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            var parsedFromText = ParseTurkishDate(text);
            if (parsedFromText != DateTime.MinValue)
            {
                return parsedFromText;
            }
        }

        return ParseTurkishDate(valueText ?? string.Empty);
    }

    public static double ParseDoubleCell(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        if (value is double d)
        {
            return d;
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var trCulture = new CultureInfo("tr-TR");
        if (double.TryParse(text, NumberStyles.Any, trCulture, out var result))
        {
            return result;
        }

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return 0;
    }

    public static double ParsePercentCell(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        text = text.Replace("%", "");
        var trCulture = new CultureInfo("tr-TR");
        if (double.TryParse(text, NumberStyles.Any, trCulture, out var result))
        {
            return result;
        }

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return 0;
    }

    public static int ParseUretimAdedi(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is double d)
        {
            return (int)Math.Round(d, MidpointRounding.AwayFromZero);
        }

        if (value is DateTime)
        {
            return 0;
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var normalized = text.ToLowerInvariant().Replace(",", ".");

        var match = Regex.Match(normalized, @"-?\d+(\.\d+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            return (int)Math.Round(number, MidpointRounding.AwayFromZero);
        }

        if (normalized.Contains("yarım"))
        {
            return 1;
        }

        return 0;
    }

    public static int ParseCalismaSuresiDakika(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is double d)
        {
            if (d > 0 && d < 1)
            {
                return (int)Math.Round(d * 24 * 60, MidpointRounding.AwayFromZero);
            }
            return (int)Math.Round(d, MidpointRounding.AwayFromZero);
        }

        if (value is DateTime dt)
        {
            return (int)Math.Round(dt.TimeOfDay.TotalMinutes, MidpointRounding.AwayFromZero);
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var normalized = text.ToLowerInvariant();
        normalized = normalized.Replace(",", ".").Replace(" ", "");
        normalized = normalized.Replace("saaat", "saat").Replace("ssat", "saat");

        double totalMinutes = 0;

        if (normalized.Contains("saat"))
        {
            var hourMatch = Regex.Match(normalized, @"(\d+(\.\d+)?)saat");
            if (hourMatch.Success && double.TryParse(hourMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var hours))
            {
                totalMinutes += hours * 60;
            }
        }

        if (normalized.Contains("dk"))
        {
            var minMatch = Regex.Match(normalized, @"(\d+(\.\d+)?)dk");
            if (minMatch.Success && double.TryParse(minMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mins))
            {
                totalMinutes += mins;
            }
        }

        if (totalMinutes > 0)
        {
            return (int)Math.Round(totalMinutes, MidpointRounding.AwayFromZero);
        }

        if (normalized.Contains("yarım") && normalized.Contains("saat"))
        {
            return 30;
        }

        if (normalized.Contains("boy"))
        {
            return 0;
        }

        var fallbackMatch = Regex.Match(normalized, @"-?\d+(\.\d+)?");
        if (fallbackMatch.Success && double.TryParse(fallbackMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var fallback))
        {
            return (int)Math.Round(fallback, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    public static double NormalizePercentValue(double value)
    {
        if (value <= 0)
        {
            return 0;
        }

        if (value <= 1.0)
        {
            return Math.Round(value * 100, 2);
        }

        return value > 100 ? 100 : value;
    }

    public static void AddDuraklama(Dictionary<string, double> toplamlar, string? neden, double dakika)
    {
        if (string.IsNullOrWhiteSpace(neden) || dakika <= 0)
        {
            return;
        }

        var trimmed = neden.Trim();
        if (trimmed == "0" || trimmed == "0,0" || trimmed == "0.0")
        {
            return;
        }

        if (trimmed.All(ch => char.IsDigit(ch) || ch == ',' || ch == '.'))
        {
            return;
        }

        if (toplamlar.ContainsKey(trimmed))
        {
            toplamlar[trimmed] += dakika;
        }
        else
        {
            toplamlar[trimmed] = dakika;
        }
    }

    public static int? ResolveYearForMonth(IEnumerable<DateTime> dates, int month, int? requestedYear)
    {
        var monthDates = dates.Where(d => d.Month == month).ToList();
        if (!monthDates.Any())
        {
            return null;
        }

        if (requestedYear.HasValue && monthDates.Any(d => d.Year == requestedYear.Value))
        {
            return requestedYear.Value;
        }

        return monthDates.Max(d => d.Year);
    }

    public static string NormalizeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Bilinmeyen";
        }

        var trimmed = value.Trim();
        var culture = new CultureInfo("tr-TR");
        var lower = trimmed.ToLower(culture);
        return culture.TextInfo.ToTitleCase(lower);
    }
}
