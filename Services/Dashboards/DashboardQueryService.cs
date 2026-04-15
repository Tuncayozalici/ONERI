using ONERI.Models;

namespace ONERI.Services.Dashboards;

public class DashboardQueryService : IDashboardQueryService
{
    private readonly IDashboardIngestionService _ingestionService;

    public DashboardQueryService(IDashboardIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    private static DateTime ResolveClosestAvailableDate(IEnumerable<DateTime> dates, DateTime referenceDate)
    {
        var available = dates
            .Where(x => x != DateTime.MinValue)
            .Select(x => x.Date)
            .Distinct()
            .ToList();

        if (!available.Any())
        {
            return referenceDate.Date;
        }

        return available
            .OrderBy(x => Math.Abs((x - referenceDate.Date).TotalDays))
            .ThenByDescending(x => x)
            .First();
    }

    private static (DateTime? Baslangic, DateTime? Bitis) NormalizeDateRange(DateTime? baslangicTarihi, DateTime? bitisTarihi)
    {
        if (!baslangicTarihi.HasValue || !bitisTarihi.HasValue)
        {
            return (null, null);
        }

        var start = baslangicTarihi.Value.Date;
        var end = bitisTarihi.Value.Date;
        return start <= end ? (start, end) : (end, start);
    }

    private static DateTime? ResolveLastAvailableDateInRange(IEnumerable<DateTime> dates, DateTime startDate, DateTime endDate)
    {
        var availableDatesInRange = dates
            .Where(x => x != DateTime.MinValue)
            .Select(x => x.Date)
            .Where(x => x >= startDate.Date && x <= endDate.Date)
            .Distinct()
            .ToList();

        return availableDatesInRange.Count == 0
            ? null
            : availableDatesInRange.Max();
    }

    private static void AddFilterAvailabilityMetadata(Dictionary<string, object?> bag, IEnumerable<DateTime> dates)
    {
        var availableDates = dates
            .Where(x => x != DateTime.MinValue)
            .Select(x => x.Date)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (availableDates.Count == 0)
        {
            bag["AvailableFilterDates"] = string.Empty;
            bag["UnavailableFilterMessage"] = "Bu tarihte veri bulunmuyor. Lütfen daha eski bir tarih seçin.";
            return;
        }

        var latestAvailableDate = availableDates[^1];
        bag["AvailableFilterDates"] = string.Join(",", availableDates.Select(x => x.ToString("yyyy-MM-dd")));
        bag["UnavailableFilterMessage"] = $"Bu tarihte veri bulunmuyor. Lütfen {latestAvailableDate:dd.MM.yyyy} veya daha eski bir tarih seçin.";
    }

    private static int CountDistinctWorkingDays(IEnumerable<DateTime> dates)
    {
        return dates
            .Where(x => x != DateTime.MinValue)
            .Select(x => x.Date)
            .Distinct()
            .Count();
    }

    private static List<(DateTime Tarih, string Bolum, int Personel)> NormalizePersonelRows(IEnumerable<PersonelYoklamaSatirModel> rows)
    {
        return rows
            .Where(x => x.Tarih != DateTime.MinValue && !string.IsNullOrWhiteSpace(x.BolumAdi))
            .Select(x => (
                Tarih: x.Tarih.Date,
                Bolum: DashboardParsingHelper.NormalizeLabel(x.BolumAdi),
                Personel: Math.Max(0, x.PersonelSayisi)))
            .ToList();
    }

    private static int RoundPersonnelAverage(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static int CalculateRoundedAveragePersonnel(
        IEnumerable<(DateTime Tarih, string Bolum, int Personel)> rows,
        DateTime startDate,
        DateTime endDate,
        Func<string, bool>? bolumFilter = null)
    {
        var dailyTotals = rows
            .Where(x => x.Tarih >= startDate.Date && x.Tarih <= endDate.Date && x.Personel > 0)
            .Where(x => bolumFilter == null || bolumFilter(x.Bolum))
            .GroupBy(x => x.Tarih)
            .Select(g => g.Sum(x => x.Personel))
            .ToList();

        return dailyTotals.Count == 0
            ? 0
            : RoundPersonnelAverage(dailyTotals.Average());
    }

    private static bool IsProfilLazerDepartment(string bolum)
    {
        return bolum.Contains("metal", StringComparison.OrdinalIgnoreCase)
            || bolum.Contains("profil", StringComparison.OrdinalIgnoreCase)
            || bolum.Contains("lazer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBoyahaneDepartment(string bolum)
    {
        return bolum.Contains("boya", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCncDepartment(string bolum)
    {
        return bolum.Contains("cnc", StringComparison.OrdinalIgnoreCase)
            || bolum.Contains("delik", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPvcDepartment(string bolum)
    {
        return bolum.Contains("pvc", StringComparison.OrdinalIgnoreCase)
            || bolum.Contains("bantlama", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEbatlamaDepartment(string bolum)
    {
        return bolum.Contains("ebatlama", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<DashboardPageResult<GenelFabrikaOzetViewModel>> GetGunlukVerilerAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var model = new GenelFabrikaOzetViewModel();
        var bag = new Dictionary<string, object?>();

        var profilRows = snapshot.ProfilRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Makine: x.CalisilanMakine,
                Uretim: (double)x.UretimAdedi,
                Duraklama: (double)(x.KalanSure > 0
                    ? x.KalanSure
                    : x.DuraklamaSuresi1 + x.DuraklamaSuresi2 + x.DuraklamaSuresi3)))
            .ToList();

        var boyaRows = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Makine: DashboardParsingHelper.NormalizeLabel(x.Makine),
                Uretim: (double)x.ToplamBoyananParca,
                Duraklama: (double)x.DuraklamaDakikaToplam,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var boyaHataFromUretimRows = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue && x.HataliParcaSayisi > 0)
            .Select(x => (
                x.Tarih,
                Adet: (double)x.HataliParcaSayisi,
                M2: 0d,
                Neden: (string?)(string.IsNullOrWhiteSpace(x.Aciklama) ? "Boyahane Hatalı Parça" : x.Aciklama),
                Bolum: (string?)"Boyahane",
                Operator: (string?)null))
            .ToList();

        var boyaHataLegacyRows = snapshot.BoyaHataRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Adet: (double)x.HataliAdet,
                M2: 0d,
                Neden: x.HataNedeni,
                Bolum: (string?)"Boyahane",
                Operator: (string?)null))
            .ToList();

        var pvcRows = snapshot.PvcRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Makine: x.Makine,
                Uretim: x.ParcaSayisi,
                Duraklama: x.Duraklama1 + x.Duraklama2 + x.Duraklama3,
                Fiili: x.FiiliCalismaOrani,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var masterRows = snapshot.MasterwoodRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Uretim: x.DelikFreezeSayisi,
                Duraklama: x.Duraklama1 + x.Duraklama2 + x.Duraklama3,
                Fiili: x.FiiliCalismaOrani,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var skipperRows = snapshot.SkipperRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Uretim: x.DelikSayisi,
                Duraklama: x.Duraklama1 + x.Duraklama2 + x.Duraklama3,
                Fiili: x.FiiliCalismaOrani,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var roverBRows = snapshot.RoverBRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Uretim: x.DelikFreezePvcSayisi,
                Duraklama: x.Duraklama1 + x.Duraklama2 + x.Duraklama3 + x.Duraklama4,
                Fiili: x.FiiliCalismaOrani,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var tezgahRows = snapshot.TezgahRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, Uretim: x.ParcaAdeti, Duraklama: x.KayipSureDakika, Kullanilabilirlik: x.Kullanilabilirlik))
            .ToList();

        var ebatlamaRows = snapshot.EbatlamaRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (
                x.Tarih,
                Makine: x.Makine,
                Uretim: x.Plaka8Mm + x.Plaka18Mm + x.Plaka30Mm,
                Duraklama: x.Duraklama1 + x.Duraklama2,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var personelRows = snapshot.PersonelRows
            .Where(x => x.Tarih != DateTime.MinValue && !string.IsNullOrWhiteSpace(x.BolumAdi))
            .Select(x => (
                x.Tarih,
                Bolum: DashboardParsingHelper.NormalizeLabel(x.BolumAdi),
                Personel: Math.Max(0, x.PersonelSayisi)))
            .ToList();

        var gunlukCalismaRows = snapshot.GunlukCalismaRows
            .Where(x => x.Tarih != DateTime.MinValue && !string.IsNullOrWhiteSpace(x.BolumAdi))
            .Select(x => (
                Tarih: x.Tarih.Date,
                Bolum: x.BolumAdi!.Trim(),
                PlanUyumOrani: DashboardParsingHelper.NormalizePercentValue(x.PlanUyumOrani),
                ToplamModulSayisi: Math.Max(0, x.ToplamModulSayisi)))
            .ToList();

        var hataliRows = new List<(DateTime Tarih, double Adet, double M2, string? Neden, string? Bolum, string? Operator)>();
        var hasLegacyProfilHataRows = snapshot.ProfilHataRows.Any(x =>
            x.Tarih != DateTime.MinValue
            && IsProfilLazerHataBolumu(x.BolumAdi));

        hataliRows.AddRange(snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, x.Adet, x.ToplamM2, x.HataNedeni, x.BolumAdi, x.OperatorAdi)));

        if (hasLegacyProfilHataRows)
        {
            hataliRows.AddRange(snapshot.ProfilHataRows
                .Where(x => x.Tarih != DateTime.MinValue)
                .Select(x => (x.Tarih, (double)x.Adet, 0d, x.HataNedeni, x.BolumAdi, (string?)null)));
        }
        else
        {
            hataliRows.AddRange(snapshot.ProfilRows
                .Where(x => x.Tarih != DateTime.MinValue && x.HataSayisi > 0)
                .Select(x => (x.Tarih, (double)x.HataSayisi, 0d, (string?)"Metal Hata Sayısı", (string?)"Profil Lazer", (string?)null)));
        }

        if (boyaHataFromUretimRows.Any())
        {
            hataliRows.AddRange(boyaHataFromUretimRows);
        }
        else
        {
            hataliRows.AddRange(boyaHataLegacyRows);
        }

        var allDates = profilRows.Select(x => x.Tarih.Date)
            .Concat(boyaRows.Select(x => x.Tarih.Date))
            .Concat(pvcRows.Select(x => x.Tarih.Date))
            .Concat(masterRows.Select(x => x.Tarih.Date))
            .Concat(skipperRows.Select(x => x.Tarih.Date))
            .Concat(roverBRows.Select(x => x.Tarih.Date))
            .Concat(tezgahRows.Select(x => x.Tarih.Date))
            .Concat(ebatlamaRows.Select(x => x.Tarih.Date))
            .Concat(personelRows.Select(x => x.Tarih.Date))
            .Concat(gunlukCalismaRows.Select(x => x.Tarih))
            .Concat(hataliRows.Select(x => x.Tarih.Date))
            .ToList();
        AddFilterAvailabilityMetadata(bag, allDates);

        var maxDate = allDates.Any() ? allDates.Max() : DateTime.Today;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;
        var isSingleDayRange = hasDateRange && rangeStart!.Value.Date == rangeEnd!.Value.Date;
        var isWholeMonthRange = hasDateRange
            && rangeStart!.Value.Year == rangeEnd!.Value.Year
            && rangeStart.Value.Month == rangeEnd.Value.Month
            && rangeStart.Value.Day == 1
            && rangeEnd.Value.Day == DateTime.DaysInMonth(rangeEnd.Value.Year, rangeEnd.Value.Month);

        DateTime ozetStart;
        DateTime ozetEnd;
        DateTime trendStart;
        DateTime trendEnd;
        if (hasDateRange)
        {
            ozetStart = rangeStart!.Value;
            ozetEnd = rangeEnd!.Value;

            if (isWholeMonthRange)
            {
                var latestAvailableDate = ResolveLastAvailableDateInRange(allDates, ozetStart, ozetEnd);
                if (latestAvailableDate.HasValue && latestAvailableDate.Value != DateTime.MinValue && latestAvailableDate.Value < ozetEnd)
                {
                    ozetEnd = latestAvailableDate.Value;
                    bag["SelectedFilterPeriodText"] = $"{ozetStart:dd MMMM yyyy} - {ozetEnd:dd MMMM yyyy}";
                }
            }

            if (isSingleDayRange)
            {
                // Tek gün seçiminde trendler seçilen gün + önceki 5 gün olarak gösterilir.
                trendEnd = ozetEnd;
                trendStart = ozetEnd.AddDays(-5);
                bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy}";
            }
            else
            {
                trendStart = ozetStart;
                trendEnd = ozetEnd;
                bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy} - {ozetEnd:dd.MM.yyyy}";
            }
        }
        else if (ay.HasValue)
        {
            var resolvedYear = DashboardParsingHelper.ResolveYearForMonth(allDates, ay.Value, yil);
            var yearToUse = resolvedYear ?? yil ?? maxDate.Year;
            if (resolvedYear.HasValue && (!yil.HasValue || yil.Value != resolvedYear.Value))
            {
                bag["OzetResolvedYear"] = resolvedYear.Value;
            }

            ozetStart = new DateTime(yearToUse, ay.Value, 1);
            var requestedMonthEnd = ozetStart.AddMonths(1).AddDays(-1);
            var latestAvailableDate = ResolveLastAvailableDateInRange(allDates, ozetStart, requestedMonthEnd);
            ozetEnd = latestAvailableDate.HasValue && latestAvailableDate.Value != DateTime.MinValue
                ? latestAvailableDate.Value
                : requestedMonthEnd;
            trendStart = ozetStart;
            trendEnd = ozetEnd;
            bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy} - {ozetEnd:dd.MM.yyyy}";

            if (ozetEnd < requestedMonthEnd)
            {
                bag["SelectedFilterPeriodText"] = $"{ozetStart:dd MMMM yyyy} - {ozetEnd:dd MMMM yyyy}";
            }
        }
        else if (raporTarihi.HasValue)
        {
            ozetStart = raporTarihi.Value.Date;
            ozetEnd = ozetStart;
            trendEnd = ozetEnd;
            trendStart = trendEnd.AddDays(-6);
            bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy}";
        }
        else
        {
            var varsayilanOzetTarihi = ResolveClosestAvailableDate(allDates, DateTime.Today.AddDays(-1));
            ozetEnd = varsayilanOzetTarihi;
            ozetStart = varsayilanOzetTarihi;
            trendStart = varsayilanOzetTarihi.AddDays(-6);
            trendEnd = ozetEnd;
            bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy}";
        }

        var ozetTarihleri = Enumerable.Range(0, (ozetEnd - ozetStart).Days + 1)
            .Select(offset => ozetStart.AddDays(offset))
            .ToList();
        model.CalisilanIsGunu = CountDistinctWorkingDays(allDates.Where(x => x.Date >= ozetStart && x.Date <= ozetEnd));
        model.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, ozetStart, ozetEnd);
        model.ToplamModulSayisi = gunlukCalismaRows
            .Where(x => x.Tarih >= ozetStart && x.Tarih <= ozetEnd)
            .Sum(x => x.ToplamModulSayisi);

        var trendTarihleri = Enumerable.Range(0, (trendEnd - trendStart).Days + 1)
            .Select(offset => trendStart.AddDays(offset))
            .ToList();

        var uretimGunluk = ozetTarihleri.ToDictionary(t => t, _ => 0d);
        var hataGunluk = ozetTarihleri.ToDictionary(t => t, _ => 0d);
        var duraklamaGunluk = ozetTarihleri.ToDictionary(t => t, _ => 0d);
        var modulTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var uretimTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var hataTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var duraklamaTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var bolumKatki = new Dictionary<string, double>();
        var uretimDetayMap = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        var hataDetayMap = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        var duraklamaDetayMap = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

        static void AddDaily(Dictionary<DateTime, double> dict, DateTime date, double value)
        {
            var d = date.Date;
            if (dict.ContainsKey(d))
            {
                dict[d] += value;
            }
        }

        static void AddDept(Dictionary<string, double> dict, string key, double value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] += value;
            }
            else
            {
                dict[key] = value;
            }
        }

        foreach (var row in gunlukCalismaRows.Where(x => x.Tarih >= trendStart && x.Tarih <= trendEnd && x.ToplamModulSayisi > 0))
        {
            AddDaily(modulTrendGunluk, row.Tarih, row.ToplamModulSayisi);
        }

        static void AddDuraklamaDetay(Dictionary<string, Dictionary<string, double>> dict, string bolum, string makine, double dakika)
        {
            if (dakika <= 0)
            {
                return;
            }

            if (!dict.TryGetValue(bolum, out var makineMap))
            {
                makineMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                dict[bolum] = makineMap;
            }

            if (makineMap.ContainsKey(makine))
            {
                makineMap[makine] += dakika;
            }
            else
            {
                makineMap[makine] = dakika;
            }
        }

        static void AddKpiDetay(Dictionary<string, Dictionary<string, double>> dict, string bolum, string makine, double deger)
        {
            if (deger <= 0)
            {
                return;
            }

            if (!dict.TryGetValue(bolum, out var makineMap))
            {
                makineMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                dict[bolum] = makineMap;
            }

            if (makineMap.ContainsKey(makine))
            {
                makineMap[makine] += deger;
            }
            else
            {
                makineMap[makine] = deger;
            }
        }

        static List<KpiBolumDetayModel> BuildKpiDetayList(Dictionary<string, Dictionary<string, double>> dict)
        {
            return dict
                .Select(bolum => new KpiBolumDetayModel
                {
                    Bolum = bolum.Key,
                    ToplamDeger = bolum.Value.Values.Sum(),
                    MakineDetaylari = bolum.Value
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => x.Key)
                        .Select(x => new KpiMakineDetayModel
                        {
                            Makine = x.Key,
                            Deger = x.Value
                        })
                        .ToList()
                })
                .OrderByDescending(x => x.ToplamDeger)
                .ThenBy(x => x.Bolum)
                .ToList();
        }

        static bool IsProfilLazerHataBolumu(string? bolumAdi)
        {
            if (string.IsNullOrWhiteSpace(bolumAdi))
            {
                return false;
            }

            var bolum = bolumAdi.ToLowerInvariant();
            return bolum.Contains("metal") || bolum.Contains("profil") || bolum.Contains("lazer");
        }

        static bool IsMakineHatasi(string? hataNedeni)
        {
            var normalized = DashboardParsingHelper.NormalizeLabel(hataNedeni);
            return normalized.Contains("makine hatası", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsCncBolum(string? bolumAdi)
        {
            if (string.IsNullOrWhiteSpace(bolumAdi))
            {
                return false;
            }

            var normalized = DashboardParsingHelper.NormalizeLabel(bolumAdi);
            return normalized.Contains("cnc", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("delik", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsPvcBolum(string? bolumAdi)
        {
            if (string.IsNullOrWhiteSpace(bolumAdi))
            {
                return false;
            }

            var normalized = DashboardParsingHelper.NormalizeLabel(bolumAdi);
            return normalized.Contains("pvc", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("bantlama", StringComparison.OrdinalIgnoreCase);
        }

        static double ReadNumericProperty(object? value)
        {
            return value switch
            {
                null => 0,
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                decimal m => (double)m,
                _ => double.TryParse(value.ToString(), out var parsed) ? parsed : 0
            };
        }

        static int CreateDeterministicHash(string input)
        {
            unchecked
            {
                var hash = 17;
                foreach (var ch in input)
                {
                    hash = (hash * 31) + ch;
                }

                return Math.Abs(hash);
            }
        }

        static double ResolveProfilLazerOeeForGunluk(SatirModeli row)
        {
            var explicitOee = DashboardParsingHelper.NormalizePercentValue(row.Oee);
            if (explicitOee > 0)
            {
                return explicitOee;
            }

            var performans = DashboardParsingHelper.NormalizePercentValue(row.Performans);
            var kullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(row.Kullanilabilirlik);
            var kalite = DashboardParsingHelper.NormalizePercentValue(row.Kalite);

            if (performans > 0 && kullanilabilirlik > 0 && kalite > 0)
            {
                return DashboardParsingHelper.NormalizePercentValue(
                    performans * kullanilabilirlik * kalite / 10000d);
            }

            return 0;
        }

        static IEnumerable<(double Performans, double Kullanilabilirlik, double Kalite, double Oee)> ExtractMetricRows<T>(IEnumerable<T> rows, DateTime start, DateTime end)
        {
            var type = typeof(T);
            var tarihProp = type.GetProperty("Tarih");
            var performansProp = type.GetProperty("Performans");
            var kullanilabilirlikProp = type.GetProperty("Kullanilabilirlik");
            var kaliteProp = type.GetProperty("Kalite");
            var oeeProp = type.GetProperty("Oee");

            // OEE bileşenlerinden en az birinin bulunması gerekir.
            if (tarihProp == null || (performansProp == null && kaliteProp == null && oeeProp == null))
            {
                return Enumerable.Empty<(double, double, double, double)>();
            }

            var result = new List<(double Performans, double Kullanilabilirlik, double Kalite, double Oee)>();
            foreach (var row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                if (tarihProp.GetValue(row) is not DateTime tarih || tarih == DateTime.MinValue)
                {
                    continue;
                }

                var date = tarih.Date;
                if (date < start.Date || date > end.Date)
                {
                    continue;
                }

                result.Add((
                    ReadNumericProperty(performansProp?.GetValue(row)),
                    ReadNumericProperty(kullanilabilirlikProp?.GetValue(row)),
                    ReadNumericProperty(kaliteProp?.GetValue(row)),
                    ReadNumericProperty(oeeProp?.GetValue(row))
                ));
            }

            return result;
        }

        static IEnumerable<(string Bolum, double Oee)> ExtractDeptOeeRows<T>(IEnumerable<T> rows, string bolum, DateTime start, DateTime end)
        {
            var type = typeof(T);
            var tarihProp = type.GetProperty("Tarih");
            var oeeProp = type.GetProperty("Oee");
            if (tarihProp == null || oeeProp == null)
            {
                return Enumerable.Empty<(string, double)>();
            }

            var result = new List<(string Bolum, double Oee)>();
            foreach (var row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                if (tarihProp.GetValue(row) is not DateTime tarih || tarih == DateTime.MinValue)
                {
                    continue;
                }

                var date = tarih.Date;
                if (date < start.Date || date > end.Date)
                {
                    continue;
                }

                var oee = ReadNumericProperty(oeeProp.GetValue(row));
                if (oee > 0)
                {
                    result.Add((bolum, oee));
                }
            }

            return result;
        }

        static string NormalizeBoyaMakineAdiForGunluk(string? makine)
        {
            if (string.IsNullOrWhiteSpace(makine))
            {
                return "Bilinmeyen";
            }

            var trimmed = makine.Trim();
            var key = DashboardParsingHelper.NormalizeHeaderForMatch(trimmed);
            if (string.IsNullOrWhiteSpace(key))
            {
                return DashboardParsingHelper.NormalizeLabel(trimmed);
            }

            if (key.Contains("konveyor", StringComparison.OrdinalIgnoreCase))
            {
                return "Konveyör Hattı";
            }

            if (key.Contains("kucukfirin", StringComparison.OrdinalIgnoreCase))
            {
                return "Küçük Fırın";
            }

            return DashboardParsingHelper.NormalizeLabel(trimmed);
        }

        foreach (var row in profilRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "Profil Lazer", row.Uretim);
                AddKpiDetay(uretimDetayMap, "Profil Lazer", "Profil Lazer", row.Uretim);
                AddDuraklamaDetay(
                    duraklamaDetayMap,
                    "Profil Lazer",
                    string.IsNullOrWhiteSpace(row.Makine) ? "Bilinmeyen Makine" : DashboardParsingHelper.NormalizeLabel(row.Makine),
                    row.Duraklama);
            }
        }

        foreach (var row in boyaRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "Boyahane", row.Uretim);
                AddKpiDetay(uretimDetayMap, "Boyahane", NormalizeBoyaMakineAdiForGunluk(row.Makine), row.Uretim);
                AddDuraklamaDetay(duraklamaDetayMap, "Boyahane", NormalizeBoyaMakineAdiForGunluk(row.Makine), row.Duraklama);
            }
        }

        foreach (var row in pvcRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "PVC", row.Uretim);
                AddKpiDetay(
                    uretimDetayMap,
                    "PVC",
                    string.IsNullOrWhiteSpace(row.Makine) ? "Bilinmeyen Hat" : DashboardParsingHelper.NormalizeLabel(row.Makine),
                    row.Uretim);
                AddDuraklamaDetay(
                    duraklamaDetayMap,
                    "PVC",
                    string.IsNullOrWhiteSpace(row.Makine) ? "Bilinmeyen Hat" : DashboardParsingHelper.NormalizeLabel(row.Makine),
                    row.Duraklama);
            }
        }

        foreach (var row in masterRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "CNC", row.Uretim);
                AddKpiDetay(uretimDetayMap, "CNC", "Masterwood", row.Uretim);
                AddDuraklamaDetay(duraklamaDetayMap, "CNC", "Masterwood", row.Duraklama);
            }
        }

        foreach (var row in skipperRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "CNC", row.Uretim);
                AddKpiDetay(uretimDetayMap, "CNC", "Skipper", row.Uretim);
                AddDuraklamaDetay(duraklamaDetayMap, "CNC", "Skipper", row.Duraklama);
            }
        }

        foreach (var row in roverBRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "CNC", row.Uretim);
                AddKpiDetay(uretimDetayMap, "CNC", "Rover-B", row.Uretim);
                AddDuraklamaDetay(duraklamaDetayMap, "CNC", "Rover-B", row.Duraklama);
            }
        }

        foreach (var row in tezgahRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "Tezgah", row.Uretim);
                AddKpiDetay(uretimDetayMap, "Tezgah", "Makine bilgisi yok", row.Uretim);
                AddDuraklamaDetay(duraklamaDetayMap, "Tezgah", "Makine bilgisi yok", row.Duraklama);
            }
        }

        foreach (var row in ebatlamaRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            AddDaily(duraklamaTrendGunluk, row.Tarih, row.Duraklama);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDaily(duraklamaGunluk, row.Tarih, row.Duraklama);
                AddDept(bolumKatki, "Ebatlama", row.Uretim);
                AddKpiDetay(
                    uretimDetayMap,
                    "Ebatlama",
                    string.IsNullOrWhiteSpace(row.Makine) ? "Bilinmeyen Makine" : DashboardParsingHelper.NormalizeLabel(row.Makine),
                    row.Uretim);
                AddDuraklamaDetay(
                    duraklamaDetayMap,
                    "Ebatlama",
                    string.IsNullOrWhiteSpace(row.Makine) ? "Bilinmeyen Makine" : DashboardParsingHelper.NormalizeLabel(row.Makine),
                    row.Duraklama);
            }
        }

        foreach (var row in hataliRows)
        {
            AddDaily(hataTrendGunluk, row.Tarih, row.Adet);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(hataGunluk, row.Tarih, row.Adet);
            }
        }

        var filteredHatali = hataliRows.Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd).ToList();
        var profilHataDetayRows = snapshot.ProfilHataRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && IsProfilLazerHataBolumu(x.BolumAdi))
            .ToList();
        foreach (var row in profilHataDetayRows)
        {
            AddKpiDetay(hataDetayMap, "Profil Lazer", "Makine bilgisi yok", row.Adet);
        }

        var profilDashboardHataAdedi = snapshot.ProfilHataRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && IsProfilLazerHataBolumu(x.BolumAdi))
            .Sum(x => (double)x.Adet);
        if (profilDashboardHataAdedi <= 0)
        {
            var inlineProfilHataRows = snapshot.ProfilRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd
                    && x.HataSayisi > 0)
                .ToList();

            profilDashboardHataAdedi = inlineProfilHataRows.Sum(x => (double)x.HataSayisi);
            foreach (var row in inlineProfilHataRows)
            {
                AddKpiDetay(
                    hataDetayMap,
                    "Profil Lazer",
                    string.IsNullOrWhiteSpace(row.CalisilanMakine) ? "Makine bilgisi yok" : DashboardParsingHelper.NormalizeLabel(row.CalisilanMakine),
                    row.HataSayisi);
            }
        }

        var boyahaneHataSatirlari = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && x.HataliParcaSayisi > 0)
            .ToList();
        var boyahaneDashboardHataAdedi = boyahaneHataSatirlari.Sum(x => (double)x.HataliParcaSayisi);
        if (boyahaneDashboardHataAdedi > 0)
        {
            foreach (var row in boyahaneHataSatirlari)
            {
                AddKpiDetay(hataDetayMap, "Boyahane", NormalizeBoyaMakineAdiForGunluk(row.Makine), row.HataliParcaSayisi);
            }
        }
        else
        {
            var boyaLegacyHataSatirlari = snapshot.BoyaHataRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd)
                .ToList();
            boyahaneDashboardHataAdedi = boyaLegacyHataSatirlari.Sum(x => (double)x.HataliAdet);
            foreach (var row in boyaLegacyHataSatirlari)
            {
                AddKpiDetay(hataDetayMap, "Boyahane", "Makine bilgisi yok", row.HataliAdet);
            }
        }

        var boyahaneMakineDisiHataSatirlari = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && x.HataliParcaSayisi > 0)
            .ToList();
        var boyahaneHataKatkisi = boyahaneMakineDisiHataSatirlari.Sum(x => (double)x.HataliParcaSayisi);
        if (boyahaneHataKatkisi > 0)
        {
            foreach (var row in boyahaneMakineDisiHataSatirlari)
            {
                AddKpiDetay(hataDetayMap, "Boyahane", NormalizeBoyaMakineAdiForGunluk(row.Makine), row.HataliParcaSayisi);
            }
        }
        else
        {
            var boyaLegacyMakineDisiHataSatirlari = snapshot.BoyaHataRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd
                    && !IsMakineHatasi(x.HataNedeni))
                .ToList();
            boyahaneHataKatkisi = boyaLegacyMakineDisiHataSatirlari.Sum(x => (double)x.HataliAdet);
            foreach (var row in boyaLegacyMakineDisiHataSatirlari)
            {
                AddKpiDetay(hataDetayMap, "Boyahane", "Makine bilgisi yok", row.HataliAdet);
            }
        }

        var cncGenelHataToplami = snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && !IsMakineHatasi(x.HataNedeni)
                && IsCncBolum(x.BolumAdi))
            .Sum(x => x.Adet);
        var pvcGenelHataRows = snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && !IsMakineHatasi(x.HataNedeni)
                && IsPvcBolum(x.BolumAdi))
            .ToList();
        var pvcGenelHataToplami = pvcGenelHataRows.Sum(x => x.Adet);
        var pvcBolumAdi = pvcGenelHataRows
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.BolumAdi))
            .OrderByDescending(g => g.Sum(x => x.Adet))
            .Select(g => g.Key)
            .FirstOrDefault() ?? "PVC";

        foreach (var row in snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && !IsMakineHatasi(x.HataNedeni)
                && !IsCncBolum(x.BolumAdi)
                && !IsPvcBolum(x.BolumAdi)))
        {
            AddKpiDetay(
                hataDetayMap,
                DashboardParsingHelper.NormalizeLabel(row.BolumAdi),
                "Makine bilgisi yok",
                row.Adet);
        }

        foreach (var row in snapshot.ProfilHataRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && !IsMakineHatasi(x.HataNedeni)))
        {
            var bolumAdi = IsProfilLazerHataBolumu(row.BolumAdi)
                ? "Profil Lazer"
                : DashboardParsingHelper.NormalizeLabel(row.BolumAdi);
            AddKpiDetay(hataDetayMap, bolumAdi, "Makine bilgisi yok", row.Adet);
        }

        if (cncGenelHataToplami > 0)
        {
            var masterwoodCncHata = snapshot.MasterwoodRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd)
                .Sum(x => x.HataliParca);
            var skipperCncHata = snapshot.SkipperRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd)
                .Sum(x => x.HataliParca);
            var roverBCncHata = snapshot.RoverBRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd)
                .Sum(x => x.HataliParca);

            var cncMakineToplami = masterwoodCncHata + skipperCncHata + roverBCncHata;
            if (cncMakineToplami > 0)
            {
                var oran = cncMakineToplami > cncGenelHataToplami
                    ? cncGenelHataToplami / cncMakineToplami
                    : 1d;
                AddKpiDetay(hataDetayMap, "Cnc (Delik)", "Masterwood", masterwoodCncHata * oran);
                AddKpiDetay(hataDetayMap, "Cnc (Delik)", "Skipper", skipperCncHata * oran);
                AddKpiDetay(hataDetayMap, "Cnc (Delik)", "Rover-B", roverBCncHata * oran);

                var eslesmeyenHata = cncGenelHataToplami - (cncMakineToplami * oran);
                if (eslesmeyenHata > 0.001)
                {
                    AddKpiDetay(hataDetayMap, "Cnc (Delik)", "Makine eslesmedi", eslesmeyenHata);
                }
            }
            else
            {
                AddKpiDetay(hataDetayMap, "Cnc (Delik)", "Makine bilgisi yok", cncGenelHataToplami);
            }
        }

        if (pvcGenelHataToplami > 0)
        {
            var pvcMakineHataGruplari = snapshot.PvcRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd
                    && x.HataliParca > 0)
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Makine) ? "Bilinmeyen Hat" : DashboardParsingHelper.NormalizeLabel(x.Makine))
                .Select(g => new
                {
                    Makine = g.Key,
                    Toplam = g.Sum(x => x.HataliParca)
                })
                .OrderByDescending(x => x.Toplam)
                .ToList();

            var pvcMakineHataToplami = pvcMakineHataGruplari.Sum(x => x.Toplam);
            if (pvcMakineHataToplami > 0)
            {
                var oran = pvcMakineHataToplami > pvcGenelHataToplami
                    ? pvcGenelHataToplami / pvcMakineHataToplami
                    : 1d;

                foreach (var grup in pvcMakineHataGruplari)
                {
                    AddKpiDetay(hataDetayMap, pvcBolumAdi, grup.Makine, grup.Toplam * oran);
                }

                var eslesmeyenHata = pvcGenelHataToplami - (pvcMakineHataToplami * oran);
                if (eslesmeyenHata > 0.001)
                {
                    AddKpiDetay(hataDetayMap, pvcBolumAdi, "Makine eslesmedi", eslesmeyenHata);
                }
            }
            else
            {
                AddKpiDetay(hataDetayMap, pvcBolumAdi, "Makine bilgisi yok", pvcGenelHataToplami);
            }
        }

        var hataliParcaDashboardHataAdedi = snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && !IsMakineHatasi(x.HataNedeni))
            .Sum(x => x.Adet)
            + snapshot.ProfilHataRows
                .Where(x => x.Tarih != DateTime.MinValue
                    && x.Tarih.Date >= ozetStart
                    && x.Tarih.Date <= ozetEnd
                    && !IsMakineHatasi(x.HataNedeni))
                .Sum(x => (double)x.Adet)
            + boyahaneHataKatkisi;

        model.ToplamUretim = uretimGunluk.Values.Sum();
        model.ToplamHataAdet = profilDashboardHataAdedi + boyahaneDashboardHataAdedi + hataliParcaDashboardHataAdedi;
        model.ToplamHataM2 = filteredHatali.Sum(x => x.M2);
        model.ToplamDuraklamaDakika = duraklamaGunluk.Values.Sum();
        model.UretimBolumDetaylari = BuildKpiDetayList(uretimDetayMap);
        model.HataBolumDetaylari = BuildKpiDetayList(hataDetayMap);
        model.DuraklamaBolumDetaylari = duraklamaDetayMap
            .Select(bolum => new DuraklamaBolumDetayModel
            {
                Bolum = bolum.Key,
                ToplamDuraklamaDakika = bolum.Value.Values.Sum(),
                MakineDetaylari = bolum.Value
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => new DuraklamaMakineDetayModel
                    {
                        Makine = x.Key,
                        DuraklamaDakika = x.Value
                    })
                    .ToList()
            })
            .OrderByDescending(x => x.ToplamDuraklamaDakika)
            .ThenBy(x => x.Bolum)
            .ToList();

        var oeeMetricRows = new List<(double Performans, double Kullanilabilirlik, double Kalite, double Oee)>();
        oeeMetricRows.AddRange(pvcRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => (x.Performans, x.Kullanilabilirlik, x.Kalite, x.Oee)));
        oeeMetricRows.AddRange(masterRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => (x.Performans, x.Kullanilabilirlik, x.Kalite, x.Oee)));
        oeeMetricRows.AddRange(skipperRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => (x.Performans, x.Kullanilabilirlik, x.Kalite, x.Oee)));
        oeeMetricRows.AddRange(roverBRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => (x.Performans, x.Kullanilabilirlik, x.Kalite, x.Oee)));
        oeeMetricRows.AddRange(ebatlamaRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => (x.Performans, x.Kullanilabilirlik, x.Kalite, x.Oee)));

        // Bu bölümler ileride OEE bileşen alanları eklenince otomatik olarak hesaba katılır.
        oeeMetricRows.AddRange(ExtractMetricRows(snapshot.ProfilRows, ozetStart, ozetEnd));
        oeeMetricRows.AddRange(ExtractMetricRows(snapshot.BoyaUretimRows, ozetStart, ozetEnd));
        oeeMetricRows.AddRange(ExtractMetricRows(snapshot.TezgahRows, ozetStart, ozetEnd));

        var performansValues = oeeMetricRows
            .Select(x => x.Performans)
            .Where(x => x > 0)
            .ToList();
        model.OrtalamaPerformans = performansValues.Any() ? performansValues.Average() : 0;

        var kullanilabilirlikValues = oeeMetricRows
            .Select(x => x.Kullanilabilirlik)
            .Where(x => x > 0)
            .ToList();
        model.OrtalamaKullanilabilirlik = kullanilabilirlikValues.Any() ? kullanilabilirlikValues.Average() : 0;

        var kaliteValues = oeeMetricRows
            .Select(x => x.Kalite)
            .Where(x => x > 0)
            .ToList();
        model.OrtalamaKalite = kaliteValues.Any() ? kaliteValues.Average() : 0;

        var oeeValues = oeeMetricRows
            .Select(x => x.Oee)
            .Where(x => x > 0)
            .ToList();
        model.OrtalamaOee = model.OrtalamaPerformans > 0
            && model.OrtalamaKullanilabilirlik > 0
            && model.OrtalamaKalite > 0
                ? DashboardParsingHelper.NormalizePercentValue(
                    model.OrtalamaPerformans * model.OrtalamaKullanilabilirlik * model.OrtalamaKalite / 10000d)
                : (oeeValues.Any() ? oeeValues.Average() : 0);

        var machineOeeRows = new List<(string Machine, double Oee)>();
        machineOeeRows.AddRange(snapshot.ProfilRows
            .Where(x => x.Tarih != DateTime.MinValue
                && x.Tarih.Date >= ozetStart
                && x.Tarih.Date <= ozetEnd
                && !string.IsNullOrWhiteSpace(x.CalisilanMakine))
            .Select(x => (
                Machine: DashboardParsingHelper.NormalizeLabel(x.CalisilanMakine),
                Oee: ResolveProfilLazerOeeForGunluk(x)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Machine) && !x.Machine.Equals("Bilinmeyen", StringComparison.OrdinalIgnoreCase)));
        machineOeeRows.AddRange(boyaRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x =>
            {
                var makineAdi = NormalizeBoyaMakineAdiForGunluk(x.Makine);
                return (Makine: makineAdi, Oee: x.Oee);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Makine) && !x.Makine.Equals("Bilinmeyen", StringComparison.OrdinalIgnoreCase)));
        machineOeeRows.AddRange(pvcRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => (DashboardParsingHelper.NormalizeLabel(x.Makine), x.Oee)));
        machineOeeRows.AddRange(masterRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("Masterwood", x.Oee)));
        machineOeeRows.AddRange(skipperRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("Skipper", x.Oee)));
        machineOeeRows.AddRange(roverBRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("Rover-B", x.Oee)));
        machineOeeRows.AddRange(ebatlamaRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => (DashboardParsingHelper.NormalizeLabel(x.Makine), x.Oee))
            .Where(x => x.Item1 != "Bilinmeyen"));

        var machineOeeSummary = machineOeeRows
            .GroupBy(x => x.Machine)
            .Select(g => new
            {
                Makine = g.Key,
                OrtalamaOee = g.Where(v => v.Oee > 0).Select(v => v.Oee).DefaultIfEmpty(0).Average()
            })
            .OrderByDescending(x => x.OrtalamaOee)
            .ToList();

        var zorunluMakineSirasi = new[] { "Konveyör Hattı", "Küçük Fırın" };
        var zorunluMakineDictionary = boyaRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && !string.IsNullOrWhiteSpace(x.Makine))
            .Select(x => new
            {
                Makine = NormalizeBoyaMakineAdiForGunluk(x.Makine),
                Oee = x.Oee
            })
            .Where(x => zorunluMakineSirasi.Contains(x.Makine, StringComparer.OrdinalIgnoreCase))
            .GroupBy(x => x.Makine)
            .ToDictionary(
                g => g.Key,
                g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var makineAdi in zorunluMakineSirasi)
        {
            if (!machineOeeSummary.Any(x => x.Makine.Equals(makineAdi, StringComparison.OrdinalIgnoreCase)))
            {
                machineOeeSummary.Add(new
                {
                    Makine = makineAdi,
                    OrtalamaOee = zorunluMakineDictionary.TryGetValue(makineAdi, out var oee) ? oee : 0
                });
            }
        }

        machineOeeSummary = machineOeeSummary.OrderByDescending(x => x.OrtalamaOee).ToList();

        model.MakineOeeLabels = machineOeeSummary.Select(x => x.Makine).ToList();
        model.MakineOeeData = machineOeeSummary.Select(x => x.OrtalamaOee).ToList();

        var bolumOeeRows = new List<(string Bolum, double Oee)>();
        bolumOeeRows.AddRange(pvcRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("PVC", x.Oee)));
        bolumOeeRows.AddRange(masterRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("CNC", x.Oee)));
        bolumOeeRows.AddRange(skipperRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("CNC", x.Oee)));
        bolumOeeRows.AddRange(roverBRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("CNC", x.Oee)));
        bolumOeeRows.AddRange(ebatlamaRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
            .Select(x => ("Ebatlama", x.Oee)));

        bolumOeeRows.AddRange(ExtractDeptOeeRows(snapshot.ProfilRows, "Profil Lazer", ozetStart, ozetEnd));
        bolumOeeRows.AddRange(ExtractDeptOeeRows(snapshot.BoyaUretimRows, "Boyahane", ozetStart, ozetEnd));
        bolumOeeRows.AddRange(ExtractDeptOeeRows(snapshot.TezgahRows, "Tezgah", ozetStart, ozetEnd));

        if (!bolumOeeRows.Any(x => x.Bolum.Equals("Boyahane", StringComparison.OrdinalIgnoreCase)))
        {
            var boyahaneOee = boyaRows
                .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd && x.Oee > 0)
                .Select(x => x.Oee)
                .DefaultIfEmpty(0)
                .Average();

            if (boyahaneOee > 0)
            {
                bolumOeeRows.Add(("Boyahane", boyahaneOee));
            }
        }

        var bolumOeeSummary = bolumOeeRows
            .GroupBy(x => x.Bolum)
            .Select(g => new
            {
                Bolum = g.Key,
                OrtalamaOee = g.Select(v => v.Oee).DefaultIfEmpty(0).Average()
            })
            .OrderByDescending(x => x.OrtalamaOee)
            .ToList();

        model.BolumOeeLabels = bolumOeeSummary.Select(x => x.Bolum).ToList();
        model.BolumOeeData = bolumOeeSummary.Select(x => x.OrtalamaOee).ToList();

        var fiiliValues = pvcRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .Select(x => x.Fiili)
            .Concat(masterRows.Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd).Select(x => x.Fiili))
            .Concat(skipperRows.Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd).Select(x => x.Fiili))
            .Concat(roverBRows.Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd).Select(x => x.Fiili))
            .Where(x => x > 0)
            .ToList();

        model.OrtalamaFiiliCalisma = fiiliValues.Any() ? fiiliValues.Average() : 0;

        var topNeden = filteredHatali
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Neden))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (topNeden != null)
        {
            model.EnCokHataNedeni = $"{topNeden.Key} ({topNeden.Total:N0})";
        }

        var bolumToplamlari = filteredHatali
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Bolum))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Key)
            .ToList();
        if (bolumToplamlari.Count > 0)
        {
            var maxToplam = bolumToplamlari[0].Total;
            var enCokBolumler = bolumToplamlari
                .Where(x => x.Total == maxToplam)
                .Select(x => x.Key)
                .ToList();
            model.EnCokHataBolum = $"{string.Join(", ", enCokBolumler)} ({maxToplam:N0})";
        }

        var topOperator = filteredHatali
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Operator))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (topOperator != null)
        {
            model.EnCokHataOperator = $"{topOperator.Key} ({topOperator.Total:N0})";
        }

        model.TrendLabels = trendTarihleri.Select(t => t.ToString("dd.MM")).ToList();
        model.ModulTrendData = trendTarihleri.Select(t => modulTrendGunluk[t]).ToList();
        model.UretimTrendData = trendTarihleri.Select(t => uretimTrendGunluk[t]).ToList();
        model.HataTrendData = trendTarihleri.Select(t => hataTrendGunluk[t]).ToList();
        model.DuraklamaTrendData = trendTarihleri.Select(t => duraklamaTrendGunluk[t]).ToList();

        var istasyonDolulukBolumleri = new[]
        {
            "Profil Lazer",
            "Boyahane",
            "PVC",
            "CNC",
            "Ebatlama",
            "Tezgah"
        }
        .Concat(bolumOeeSummary.Select(x => x.Bolum))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        model.IstasyonDolulukSerileri = istasyonDolulukBolumleri
            .Select((bolum, bolumIndex) =>
            {
                var normalizedBolum = DashboardParsingHelper.NormalizeLabel(bolum);
                var baseSeed = CreateDeterministicHash($"{normalizedBolum}:{ozetStart:yyyyMMdd}:{ozetEnd:yyyyMMdd}:{bolumIndex}");
                var bazDoluluk = 56 + (baseSeed % 28);
                var modulBaz = 34 + (baseSeed % 42);
                var seri = new IstasyonDolulukSeriModel
                {
                    Bolum = bolum
                };

                foreach (var tarih in trendTarihleri)
                {
                    var pointSeed = CreateDeterministicHash($"{normalizedBolum}:{tarih:yyyyMMdd}");
                    var modulSayisi = modulBaz + (pointSeed % 48);
                    var dalga = ((pointSeed / 17) % 23) - 11;
                    var gunEtki = (tarih.DayOfWeek == DayOfWeek.Sunday ? -8 : 0) + (tarih.DayOfWeek == DayOfWeek.Saturday ? -3 : 0);
                    var doluluk = Math.Max(42, Math.Min(97, bazDoluluk + dalga + gunEtki + (modulSayisi * 0.07)));

                    seri.ModulSayilari.Add(modulSayisi);
                    seri.DolulukOranlari.Add(Math.Round(doluluk, 2));
                }

                return seri;
            })
            .ToList();

        var bolumList = bolumKatki.OrderByDescending(x => x.Value).ToList();
        model.BolumUretimLabels = bolumList.Select(x => x.Key).ToList();
        model.BolumUretimData = bolumList.Select(x => x.Value).ToList();

        var bolumHataList = filteredHatali
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Bolum))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();
        model.BolumHataLabels = bolumHataList.Select(x => x.Key).ToList();
        model.BolumHataData = bolumHataList.Select(x => x.Total).ToList();

        var shouldAveragePersonelByBolum = hasDateRange || ay.HasValue;
        bag["PersonelBolumTitle"] = shouldAveragePersonelByBolum
            ? "Bölüm Bazlı Personel (Ortalama)"
            : "Bölüm Bazlı Personel (Toplam)";
        bag["PersonelBolumDatasetLabel"] = shouldAveragePersonelByBolum
            ? "Ortalama Personel"
            : "Personel";

        var personelBolumList = personelRows
            .Where(x => x.Tarih.Date >= ozetStart && x.Tarih.Date <= ozetEnd)
            .GroupBy(x => x.Bolum)
            .Select(g => new
            {
                Key = g.Key,
                Value = shouldAveragePersonelByBolum
                    ? g.Average(x => (double)x.Personel)
                    : g.Sum(x => (double)x.Personel)
            })
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToList();

        model.PersonelBolumLabels = personelBolumList.Select(x => x.Key).ToList();
        model.PersonelBolumData = personelBolumList.Select(x => x.Value).ToList();

        var planUyumBolumList = gunlukCalismaRows
            .Where(x => x.Tarih >= ozetStart && x.Tarih <= ozetEnd)
            .Where(x => x.PlanUyumOrani > 0)
            .GroupBy(x => x.Bolum)
            .Select(g => new
            {
                Key = g.Key,
                Value = g.Average(x => x.PlanUyumOrani)
            })
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToList();

        model.PlanUyumBolumLabels = planUyumBolumList.Select(x => x.Key).ToList();
        model.PlanUyumBolumData = planUyumBolumList.Select(x => Math.Round(x.Value, 2)).ToList();

        var hataNedenList = filteredHatali
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Neden))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .Take(6)
            .ToList();

        model.HataNedenLabels = hataNedenList.Select(x => x.Key).ToList();
        model.HataNedenData = hataNedenList.Select(x => x.Total).ToList();
        model.RaporTarihi = ozetEnd;

        return new DashboardPageResult<GenelFabrikaOzetViewModel>
        {
            Model = model,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<ProfilLazerDashboardViewModel>> GetProfilLazerAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        static double NormalizeMetric(double value) => DashboardParsingHelper.NormalizePercentValue(value);

        static double ResolveRowOee(SatirModeli row)
        {
            var explicitOee = NormalizeMetric(row.Oee);
            if (explicitOee > 0)
            {
                return explicitOee;
            }

            var performans = NormalizeMetric(row.Performans);
            var kullanilabilirlik = NormalizeMetric(row.Kullanilabilirlik);
            var kalite = NormalizeMetric(row.Kalite);

            if (performans > 0 && kullanilabilirlik > 0 && kalite > 0)
            {
                return DashboardParsingHelper.NormalizePercentValue(
                    performans * kullanilabilirlik * kalite / 10000d);
            }

            return 0;
        }

        static double AveragePositive(IEnumerable<double> values)
        {
            var positives = values.Where(x => x > 0).ToList();
            return positives.Any() ? Math.Round(positives.Average(), 2) : 0;
        }

        static List<(string Neden, int ToplamSure)> BuildProfilDuraklamaDagilimi(IEnumerable<SatirModeli> rows)
        {
            var toplamlar = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in rows
                .SelectMany(x => new[]
                {
                    new { Neden = x.DuraklamaNedeni1, Sure = x.DuraklamaSuresi1 },
                    new { Neden = x.DuraklamaNedeni2, Sure = x.DuraklamaSuresi2 },
                    new { Neden = x.DuraklamaNedeni3, Sure = x.DuraklamaSuresi3 }
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Neden) && x.Sure > 0))
            {
                DashboardParsingHelper.AddDuraklama(toplamlar, DashboardParsingHelper.NormalizeLabel(item.Neden), item.Sure);
            }

            return toplamlar
                .Select(x => (Neden: x.Key, ToplamSure: (int)Math.Round(x.Value, MidpointRounding.AwayFromZero)))
                .OrderByDescending(x => x.ToplamSure)
                .ToList();
        }

        static List<(string Label, int Value)> TakeTopBuckets(IEnumerable<(string Label, int Value)> items, int takeCount)
        {
            var ordered = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Label) && x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ToList();

            if (ordered.Count <= takeCount)
            {
                return ordered;
            }

            var topItems = ordered.Take(takeCount).ToList();
            var remaining = ordered.Skip(takeCount).Sum(x => x.Value);
            if (remaining > 0)
            {
                topItems.Add(("Diğer", remaining));
            }

            return topItems;
        }

        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();
        var viewModel = new ProfilLazerDashboardViewModel
        {
            RaporTarihi = secilenTarih ?? DateTime.Today
        };

        var excelData = snapshot.ProfilRows.Where(x => x.Tarih != DateTime.MinValue).ToList();
        var legacyHataData = snapshot.ProfilHataRows
            .Where(x => x.Tarih != DateTime.MinValue && IsProfilLazerDepartment(x.BolumAdi ?? string.Empty))
            .ToList();
        var personelRows = NormalizePersonelRows(snapshot.PersonelRows);

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<ProfilLazerDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;

        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;
        var isSingleDayRange = hasDateRange && rangeStart!.Value.Date == rangeEnd!.Value.Date;
        var allRelevantDates = excelData.Select(x => x.Tarih).ToList();
        AddFilterAvailabilityMetadata(bag, allRelevantDates);

        DateTime? effectiveRangeEnd = null;
        if (hasDateRange)
        {
            effectiveRangeEnd = ResolveLastAvailableDateInRange(allRelevantDates, rangeStart!.Value, rangeEnd!.Value);
            if (effectiveRangeEnd.HasValue && effectiveRangeEnd.Value < rangeEnd.Value)
            {
                bag["SelectedFilterPeriodText"] = $"{rangeStart.Value:dd MMMM yyyy} - {effectiveRangeEnd.Value:dd MMMM yyyy}";
            }
        }
        else if (ay.HasValue && yil.HasValue)
        {
            var monthStart = new DateTime(yil.Value, ay.Value, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            effectiveRangeEnd = ResolveLastAvailableDateInRange(allRelevantDates, monthStart, monthEnd);
            if (effectiveRangeEnd.HasValue && effectiveRangeEnd.Value < monthEnd)
            {
                bag["SelectedFilterPeriodText"] = $"{monthStart:dd MMMM yyyy} - {effectiveRangeEnd.Value:dd MMMM yyyy}";
            }
        }

        var filteredRows = excelData.AsQueryable();
        if (hasDateRange)
        {
            var effectiveEnd = effectiveRangeEnd ?? rangeEnd!.Value;
            filteredRows = filteredRows.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= effectiveEnd);
            viewModel.RaporTarihi = effectiveEnd;
        }
        else if (ay.HasValue && yil.HasValue)
        {
            var monthStart = new DateTime(yil.Value, ay.Value, 1);
            var effectiveEnd = effectiveRangeEnd ?? monthStart.AddMonths(1).AddDays(-1);
            filteredRows = filteredRows.Where(x => x.Tarih.Date >= monthStart && x.Tarih.Date <= effectiveEnd);
            viewModel.RaporTarihi = effectiveEnd;
        }
        else
        {
            filteredRows = filteredRows.Where(x => x.Tarih.Date == islenecekTarih.Date);
        }

        var filteredRowsList = filteredRows.ToList();
        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filteredRowsList.Select(x => x.Tarih));

        var personelStart = hasDateRange
            ? rangeStart!.Value
            : ay.HasValue && yil.HasValue
                ? new DateTime(yil.Value, ay.Value, 1)
                : islenecekTarih.Date;
        var personelEnd = hasDateRange
            ? effectiveRangeEnd ?? rangeEnd!.Value
            : ay.HasValue && yil.HasValue
                ? effectiveRangeEnd ?? new DateTime(yil.Value, ay.Value, 1).AddMonths(1).AddDays(-1)
                : islenecekTarih.Date;
        viewModel.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, personelStart, personelEnd, IsProfilLazerDepartment);

        viewModel.ToplamKesilenProfilBoy = filteredRowsList.Sum(x => x.KesilenProfilBoy);
        viewModel.ToplamParcaSayisi = filteredRowsList.Sum(x => x.UretimAdedi);
        viewModel.ToplamKullanilanSure = filteredRowsList.Sum(x => x.CalismaSuresi);
        viewModel.ToplamKalanSure = filteredRowsList.Sum(x => x.KalanSure);

        var performansValues = filteredRowsList.Select(x => NormalizeMetric(x.Performans)).Where(x => x > 0).ToList();
        var kullanilabilirlikValues = filteredRowsList.Select(x => NormalizeMetric(x.Kullanilabilirlik)).Where(x => x > 0).ToList();
        var kaliteValues = filteredRowsList.Select(x => NormalizeMetric(x.Kalite)).Where(x => x > 0).ToList();
        var oeeValues = filteredRowsList.Select(ResolveRowOee).Where(x => x > 0).ToList();

        viewModel.OrtalamaPerformans = AveragePositive(performansValues);
        viewModel.OrtalamaKullanilabilirlik = AveragePositive(kullanilabilirlikValues);
        viewModel.OrtalamaKalite = AveragePositive(kaliteValues);
        viewModel.OrtalamaOee = oeeValues.Any()
            ? Math.Round(oeeValues.Average(), 2)
            : (viewModel.OrtalamaPerformans > 0 && viewModel.OrtalamaKullanilabilirlik > 0 && viewModel.OrtalamaKalite > 0
                ? Math.Round(DashboardParsingHelper.NormalizePercentValue(
                    viewModel.OrtalamaPerformans * viewModel.OrtalamaKullanilabilirlik * viewModel.OrtalamaKalite / 10000d), 2)
                : 0);

        var hasInlineHataData = filteredRowsList.Any(x => x.HataSayisi > 0);
        var filteredLegacyHataData = legacyHataData.AsQueryable();
        if (hasDateRange)
        {
            var effectiveEnd = effectiveRangeEnd ?? rangeEnd!.Value;
            filteredLegacyHataData = filteredLegacyHataData.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= effectiveEnd);
        }
        else if (ay.HasValue && yil.HasValue)
        {
            var monthStart = new DateTime(yil.Value, ay.Value, 1);
            var effectiveEnd = effectiveRangeEnd ?? monthStart.AddMonths(1).AddDays(-1);
            filteredLegacyHataData = filteredLegacyHataData.Where(x => x.Tarih.Date >= monthStart && x.Tarih.Date <= effectiveEnd);
        }
        else
        {
            filteredLegacyHataData = filteredLegacyHataData.Where(x => x.Tarih.Date == islenecekTarih.Date);
        }

        var filteredLegacyHataList = filteredLegacyHataData.ToList();
        viewModel.ToplamHataSayisi = hasInlineHataData
            ? filteredRowsList.Sum(x => x.HataSayisi)
            : filteredLegacyHataList.Sum(x => x.Adet);

        var profilDagilimi = TakeTopBuckets(
            filteredRowsList
                .GroupBy(x => DashboardParsingHelper.NormalizeProfilLabel(x.ProfilTipi))
                .Select(g => (Label: g.Key, Value: g.Sum(x => x.UretimAdedi))),
            7);
        viewModel.ProfilLabels = profilDagilimi.Select(x => x.Label).ToList();
        viewModel.ProfilParcaData = profilDagilimi.Select(x => x.Value).ToList();

        var musteriDagilimi = TakeTopBuckets(
            filteredRowsList
                .Where(x => !string.IsNullOrWhiteSpace(x.MusteriAdi))
                .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.MusteriAdi))
                .Select(g => (Label: g.Key, Value: g.Sum(x => x.UretimAdedi))),
            8);
        viewModel.MusteriLabels = musteriDagilimi.Select(x => x.Label).ToList();
        viewModel.MusteriParcaData = musteriDagilimi.Select(x => x.Value).ToList();

        var mesaiDurumuDagilimi = filteredRowsList
            .Where(x => !string.IsNullOrWhiteSpace(x.MesaiDurumu))
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.MesaiDurumu))
            .Select(g => new
            {
                Mesai = g.Key,
                ToplamSure = g.Sum(x => x.CalismaSuresi)
            })
            .OrderByDescending(x => x.ToplamSure)
            .ToList();
        viewModel.MesaiDurumuLabels = mesaiDurumuDagilimi.Select(x => x.Mesai).ToList();
        viewModel.MesaiDurumuData = mesaiDurumuDagilimi.Select(x => x.ToplamSure).ToList();

        var makineOzetleri = filteredRowsList
            .Where(x => !string.IsNullOrWhiteSpace(x.CalisilanMakine))
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.CalisilanMakine))
            .Select(g => new
            {
                Makine = g.Key,
                OrtalamaOee = AveragePositive(g.Select(ResolveRowOee)),
                KullanilanSure = g.Sum(x => x.CalismaSuresi),
                KalanSure = g.Sum(x => x.KalanSure)
            })
            .OrderByDescending(x => x.OrtalamaOee)
            .ThenByDescending(x => x.KullanilanSure)
            .ToList();
        viewModel.MakineLabels = makineOzetleri.Select(x => x.Makine).ToList();
        viewModel.MakineOeeData = makineOzetleri.Select(x => x.OrtalamaOee).ToList();
        viewModel.MakineKullanilanSureData = makineOzetleri.Select(x => x.KullanilanSure).ToList();
        viewModel.MakineKalanSureData = makineOzetleri.Select(x => x.KalanSure).ToList();

        var duraklamaDagilimi = BuildProfilDuraklamaDagilimi(filteredRowsList);
        viewModel.DuraklamaNedenLabels = duraklamaDagilimi.Select(x => x.Neden).ToList();
        viewModel.DuraklamaNedenData = duraklamaDagilimi.Select(x => x.ToplamSure).ToList();
        viewModel.MakineDuraklamaDagilimlari = makineOzetleri
            .Select(x =>
            {
                var makineDagilim = BuildProfilDuraklamaDagilimi(
                    filteredRowsList.Where(row => DashboardParsingHelper.NormalizeLabel(row.CalisilanMakine) == x.Makine));

                return new MakineDuraklamaNedenDagilimModel
                {
                    Makine = x.Makine,
                    DuraklamaNedenLabels = makineDagilim.Select(y => y.Neden).ToList(),
                    DuraklamaNedenData = makineDagilim.Select(y => y.ToplamSure).ToList()
                };
            })
            .ToList();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBitis = effectiveRangeEnd ?? rangeEnd!.Value;
            trendBaslangic = isSingleDayRange ? trendBitis.AddDays(-5) : rangeStart!.Value;
            bag["MetalTrendTitle"] = isSingleDayRange
                ? "Seçili Tarih ve Önceki 5 Gün OEE Trendi"
                : "Seçili Tarih Aralığı OEE Trendi";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = effectiveRangeEnd ?? trendBaslangic.AddMonths(1).AddDays(-1);
            bag["MetalTrendTitle"] = "Aylık OEE Trendi";
        }
        else
        {
            trendBitis = islenecekTarih;
            trendBaslangic = trendBitis.AddDays(-6);
            bag["MetalTrendTitle"] = "Son 7 Günlük OEE Trendi";
        }

        var dailyTrendData = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .Select(g => new
            {
                Tarih = g.Key,
                Performans = AveragePositive(g.Select(x => NormalizeMetric(x.Performans))),
                Kullanilabilirlik = AveragePositive(g.Select(x => NormalizeMetric(x.Kullanilabilirlik))),
                Kalite = AveragePositive(g.Select(x => NormalizeMetric(x.Kalite))),
                Oee = AveragePositive(g.Select(ResolveRowOee))
            })
            .ToDictionary(x => x.Tarih);

        var hataTrendData = hasInlineHataData
            ? excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.HataSayisi))
            : legacyHataData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Adet));

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        viewModel.TrendTarihleri = tumTarihler.Select(x => x.ToString("dd.MM")).ToList();
        viewModel.PerformansTrendData = tumTarihler
            .Select(x => dailyTrendData.TryGetValue(x, out var value) ? value.Performans : 0)
            .ToList();
        viewModel.KullanilabilirlikTrendData = tumTarihler
            .Select(x => dailyTrendData.TryGetValue(x, out var value) ? value.Kullanilabilirlik : 0)
            .ToList();
        viewModel.KaliteTrendData = tumTarihler
            .Select(x => dailyTrendData.TryGetValue(x, out var value) ? value.Kalite : 0)
            .ToList();
        viewModel.OeeTrendData = tumTarihler
            .Select(x => dailyTrendData.TryGetValue(x, out var value) ? value.Oee : 0)
            .ToList();
        viewModel.HataTrendData = tumTarihler
            .Select(x => hataTrendData.TryGetValue(x, out var value) ? value : 0)
            .ToList();

        return new DashboardPageResult<ProfilLazerDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<BoyaDashboardViewModel>> GetBoyahaneAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();
        var personelRows = NormalizePersonelRows(snapshot.PersonelRows);

        static string NormalizeBoyaDuraklamaNedeni(string? neden)
        {
            if (string.IsNullOrWhiteSpace(neden))
            {
                return string.Empty;
            }

            var trimmed = neden.Trim();
            var key = DashboardParsingHelper.NormalizeHeaderForMatch(trimmed);
            if (string.IsNullOrWhiteSpace(key))
            {
                return trimmed;
            }

            if (key.Contains("makineariza", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("makinearizasi", StringComparison.OrdinalIgnoreCase))
            {
                return "Makine Arızası";
            }

            var remaining = key;
            remaining = remaining.Replace("temizlik", string.Empty, StringComparison.OrdinalIgnoreCase);
            remaining = remaining.Replace("hazirlik", string.Empty, StringComparison.OrdinalIgnoreCase);
            remaining = remaining.Replace("ilkisinma", string.Empty, StringComparison.OrdinalIgnoreCase);
            remaining = remaining.Replace("isinma", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(remaining))
            {
                return "Temizlik/Hazırlık/İlk Isınma";
            }

            return DashboardParsingHelper.NormalizeLabel(trimmed);
        }

        static string NormalizeBoyaMakineAdi(string? makine)
        {
            if (string.IsNullOrWhiteSpace(makine))
            {
                return "Bilinmeyen";
            }

            var trimmed = makine.Trim();
            var key = DashboardParsingHelper.NormalizeHeaderForMatch(trimmed);
            if (string.IsNullOrWhiteSpace(key))
            {
                return DashboardParsingHelper.NormalizeLabel(trimmed);
            }

            if (key.Contains("konveyor", StringComparison.OrdinalIgnoreCase))
            {
                return "Konveyör Hattı";
            }

            if (key.Contains("kucukfirin", StringComparison.OrdinalIgnoreCase))
            {
                return "Küçük Fırın";
            }

            return DashboardParsingHelper.NormalizeLabel(trimmed);
        }

        var viewModel = new BoyaDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var uretimListesi = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .ToList();
        var legacyHataListesi = snapshot.BoyaHataRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .ToList();

        if (!uretimListesi.Any() && !legacyHataListesi.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<BoyaDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        var tarihKaynaklari = uretimListesi
            .Select(x => x.Tarih)
            .Concat(legacyHataListesi.Select(x => x.Tarih))
            .ToList();
        AddFilterAvailabilityMetadata(bag, tarihKaynaklari);
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(tarihKaynaklari, DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;

        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliUretim = uretimListesi.AsQueryable();
        var filtreliLegacyHata = legacyHataListesi.AsQueryable();
        int? yearToUse = null;

        if (hasDateRange)
        {
            var selectedRangeStart = rangeStart!.Value;
            var selectedRangeEnd = rangeEnd!.Value;

            filtreliUretim = filtreliUretim.Where(x => x.Tarih.Date >= selectedRangeStart && x.Tarih.Date <= selectedRangeEnd);
            filtreliLegacyHata = filtreliLegacyHata.Where(x => x.Tarih.Date >= selectedRangeStart && x.Tarih.Date <= selectedRangeEnd);

            bag["BoyaRange"] = selectedRangeStart == selectedRangeEnd
                ? $"{selectedRangeStart:dd.MM.yyyy}"
                : $"{selectedRangeStart:dd.MM.yyyy} - {selectedRangeEnd:dd.MM.yyyy}";
            bag["BoyaUretimTrendTitle"] = "Toplam Boyanan Parça Trendi (Tarih Aralığı)";
            bag["BoyaOeeTrendTitle"] = "OEE Bileşen Trendi (Tarih Aralığı)";
            bag["BoyaHataTrendTitle"] = "Hata Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue)
        {
            var resolvedYear = DashboardParsingHelper.ResolveYearForMonth(tarihKaynaklari, ay.Value, yil);
            yearToUse = resolvedYear ?? yil ?? islenecekTarih.Year;
            if (resolvedYear.HasValue && (!yil.HasValue || yil.Value != resolvedYear.Value))
            {
                bag["BoyaResolvedYear"] = resolvedYear.Value;
            }

            filtreliUretim = filtreliUretim.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yearToUse.Value);
            filtreliLegacyHata = filtreliLegacyHata.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yearToUse.Value);

            var ayBaslangic = new DateTime(yearToUse.Value, ay.Value, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);
            bag["BoyaRange"] = $"{ayBaslangic:dd.MM.yyyy} - {ayBitis:dd.MM.yyyy}";
            bag["BoyaUretimTrendTitle"] = "Toplam Boyanan Parça Trendi (Aylık)";
            bag["BoyaOeeTrendTitle"] = "OEE Bileşen Trendi (Aylık)";
            bag["BoyaHataTrendTitle"] = "Hata Trendi (Aylık)";
        }
        else
        {
            filtreliUretim = filtreliUretim.Where(x => x.Tarih.Date == islenecekTarih.Date);
            filtreliLegacyHata = filtreliLegacyHata.Where(x => x.Tarih.Date == islenecekTarih.Date);

            bag["BoyaRange"] = $"{islenecekTarih:dd.MM.yyyy}";
            bag["BoyaUretimTrendTitle"] = "Toplam Boyanan Parça Trendi (Son 7 Gün)";
            bag["BoyaOeeTrendTitle"] = "OEE Bileşen Trendi (Son 7 Gün)";
            bag["BoyaHataTrendTitle"] = "Hata Trendi (Son 7 Gün)";
        }

        var seciliUretim = filtreliUretim.ToList();
        var seciliLegacyHata = filtreliLegacyHata.ToList();
        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(
            (seciliUretim.Any() ? seciliUretim.Select(x => x.Tarih) : seciliLegacyHata.Select(x => x.Tarih)));
        var boyaPersonelStart = hasDateRange
            ? rangeStart!.Value
            : ay.HasValue
                ? new DateTime(yearToUse ?? yil ?? islenecekTarih.Year, ay.Value, 1)
                : islenecekTarih.Date;
        var boyaPersonelEnd = hasDateRange
            ? rangeEnd!.Value
            : ay.HasValue
                ? new DateTime(yearToUse ?? yil ?? islenecekTarih.Year, ay.Value, 1).AddMonths(1).AddDays(-1)
                : islenecekTarih.Date;
        viewModel.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, boyaPersonelStart, boyaPersonelEnd, IsBoyahaneDepartment);

        viewModel.PanelBoyananParca = seciliUretim.Sum(x => x.PanelAdet);
        viewModel.DosemeBoyananParca = seciliUretim.Sum(x => x.DosemeAdet);
        viewModel.BuyukParcaAdedi = seciliUretim.Sum(x => x.BuyukParcaAdeti);
        viewModel.KucukParcaAdedi = seciliUretim.Sum(x => x.KucukParcaAdeti);
        viewModel.KirliProfilParca = seciliUretim.Sum(x => x.KirliProfilParcaSayisi);
        viewModel.ToplamBoyananParca = seciliUretim.Sum(x => x.ToplamBoyananParca);
        viewModel.ToplamPerformansIcinParcaSayisi = seciliUretim.Sum(x => x.PerformansIcinParcaSayisi);
        viewModel.UretimHedefGerceklesmeOrani = viewModel.ToplamPerformansIcinParcaSayisi > 0
            ? (viewModel.ToplamBoyananParca / viewModel.ToplamPerformansIcinParcaSayisi) * 100
            : 0;

        var uretimdenHataToplami = seciliUretim.Sum(x => (double)x.HataliParcaSayisi);
        viewModel.ToplamHataliParca = uretimdenHataToplami > 0
            ? uretimdenHataToplami
            : seciliLegacyHata.Sum(x => (double)x.HataliAdet);
        viewModel.HataOrani = viewModel.ToplamBoyananParca > 0
            ? (viewModel.ToplamHataliParca / viewModel.ToplamBoyananParca) * 100
            : 0;

        viewModel.ToplamDuraklamaDakika = seciliUretim.Sum(x => x.DuraklamaDakikaToplam);
        var hatHiziValues = seciliUretim.Where(x => x.HatHizi > 0).Select(x => x.HatHizi).ToList();
        viewModel.OrtalamaHatHizi = hatHiziValues.Any() ? hatHiziValues.Average() : 0;

        var performansValues = seciliUretim.Where(x => x.Performans > 0).Select(x => x.Performans).ToList();
        var kaliteValues = seciliUretim.Where(x => x.Kalite > 0).Select(x => x.Kalite).ToList();
        var kullanilabilirlikValues = seciliUretim.Where(x => x.Kullanilabilirlik > 0).Select(x => x.Kullanilabilirlik).ToList();
        var oeeValues = seciliUretim.Where(x => x.Oee > 0).Select(x => x.Oee).ToList();

        viewModel.OrtalamaPerformans = performansValues.Any() ? performansValues.Average() : 0;
        viewModel.OrtalamaKalite = kaliteValues.Any() ? kaliteValues.Average() : 0;
        viewModel.OrtalamaKullanilabilirlik = kullanilabilirlikValues.Any() ? kullanilabilirlikValues.Average() : 0;
        viewModel.OrtalamaOee = oeeValues.Any() ? oeeValues.Average() : 0;

        viewModel.ToplamKayitSayisi = seciliUretim.Count;
        viewModel.OgleArasiCalisilanKayitSayisi = seciliUretim.Count(x => x.OgleArasiCalisildi);
        viewModel.OgleArasiCalismaOrani = viewModel.ToplamKayitSayisi > 0
            ? (viewModel.OgleArasiCalisilanKayitSayisi / viewModel.ToplamKayitSayisi) * 100
            : 0;

        var parcaKarmasi = new List<(string Label, double Value)>
        {
            ("Panel", viewModel.PanelBoyananParca),
            ("Döşeme", viewModel.DosemeBoyananParca),
            ("Büyük Parça", viewModel.BuyukParcaAdedi),
            ("Küçük Parça", viewModel.KucukParcaAdedi),
            ("Kirli Profil", viewModel.KirliProfilParca)
        }.Where(x => x.Value > 0).ToList();

        viewModel.ParcaKarmaLabels = parcaKarmasi.Select(x => x.Label).ToList();
        viewModel.ParcaKarmaData = parcaKarmasi.Select(x => x.Value).ToList();

        var makineOzetMap = seciliUretim
            .GroupBy(x => NormalizeBoyaMakineAdi(x.Makine))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var makineOeeValues = g.Where(v => v.Oee > 0).Select(v => v.Oee).ToList();
                    return new MakineKartOzetModel
                    {
                        MakineAdi = g.Key,
                        Uretim = g.Sum(v => (double)v.ToplamBoyananParca),
                        HataliParca = g.Sum(v => (double)v.HataliParcaSayisi),
                        DuraklamaDakika = g.Sum(v => (double)v.DuraklamaDakikaToplam),
                        Oee = makineOeeValues.Any() ? makineOeeValues.Average() : 0
                    };
                },
                StringComparer.OrdinalIgnoreCase);

        // Boyahane tarafında makine kimliği iki ana hatta normalize edilir.
        var zorunluMakineSirasi = new[] { "Konveyör Hattı", "Küçük Fırın" };
        var ekMakineAdlari = makineOzetMap.Keys
            .Where(x => !zorunluMakineSirasi.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Where(x => !x.Equals("Bilinmeyen", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToList();

        var makineSirasi = zorunluMakineSirasi
            .Concat(ekMakineAdlari)
            .ToList();

        if (!makineSirasi.Any())
        {
            makineSirasi = zorunluMakineSirasi.ToList();
        }

        viewModel.MakineKartlari = makineSirasi
            .Select(makineAdi => makineOzetMap.TryGetValue(makineAdi, out var kart)
                ? kart
                : new MakineKartOzetModel { MakineAdi = makineAdi })
            .ToList();

        viewModel.MakineLabels = viewModel.MakineKartlari.Select(x => x.MakineAdi).ToList();
        viewModel.MakineUretimData = viewModel.MakineKartlari.Select(x => x.Uretim).ToList();
        viewModel.MakineOeeData = viewModel.MakineKartlari.Select(x => x.Oee).ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in seciliUretim)
        {
            DashboardParsingHelper.AddDuraklama(
                duraklamaNedenleri,
                NormalizeBoyaDuraklamaNedeni(row.DuraklamaNedeni1),
                row.DuraklamaSuresi1);
            DashboardParsingHelper.AddDuraklama(
                duraklamaNedenleri,
                NormalizeBoyaDuraklamaNedeni(row.DuraklamaNedeni2),
                row.DuraklamaSuresi2);
            DashboardParsingHelper.AddDuraklama(
                duraklamaNedenleri,
                NormalizeBoyaDuraklamaNedeni(row.DuraklamaNedeni3),
                row.DuraklamaSuresi3);
        }

        var duraklamaList = duraklamaNedenleri
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue)
        {
            var yearForTrend = yearToUse ?? yil ?? islenecekTarih.Year;
            trendBaslangic = new DateTime(yearForTrend, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            trendBitis = islenecekTarih;
            trendBaslangic = islenecekTarih.AddDays(-6);
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var trendKaynak = uretimListesi
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .ToList();

        var trendKaynakHataVar = trendKaynak.Any(x => x.HataliParcaSayisi > 0);

        var uretimGunluk = trendKaynak
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => (double)x.ToplamBoyananParca));
        var hedefGunluk = trendKaynak
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PerformansIcinParcaSayisi));

        var hataGunluk = trendKaynakHataVar
            ? trendKaynak
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => (double)x.HataliParcaSayisi))
            : legacyHataListesi
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => (double)x.HataliAdet));

        var performansGunluk = trendKaynak
            .Where(x => x.Performans > 0)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Performans));
        var kaliteGunluk = trendKaynak
            .Where(x => x.Kalite > 0)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Kalite));
        var kullanilabilirlikGunluk = trendKaynak
            .Where(x => x.Kullanilabilirlik > 0)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Kullanilabilirlik));
        var oeeGunluk = trendKaynak
            .Where(x => x.Oee > 0)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Oee));

        viewModel.UretimTrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.UretimTrendData = tumTarihler.Select(t => uretimGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.HedefTrendData = tumTarihler.Select(t => hedefGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.HataTrendData = tumTarihler.Select(t => hataGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.PerformansTrendData = tumTarihler.Select(t => performansGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KaliteTrendData = tumTarihler.Select(t => kaliteGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KullanilabilirlikTrendData = tumTarihler.Select(t => kullanilabilirlikGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.OeeTrendData = tumTarihler.Select(t => oeeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        return new DashboardPageResult<BoyaDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<PvcDashboardViewModel>> GetPvcAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, string? makine, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();
        var personelRows = NormalizePersonelRows(snapshot.PersonelRows);

        static string NormalizePvcDuraklamaNedeni(string? neden)
        {
            if (string.IsNullOrWhiteSpace(neden))
            {
                return string.Empty;
            }

            var trimmed = neden.Trim();
            var key = DashboardParsingHelper.NormalizeHeaderForMatch(trimmed);
            if (string.IsNullOrWhiteSpace(key))
            {
                return trimmed;
            }

            var remaining = key;
            remaining = remaining.Replace("temizlik", string.Empty, StringComparison.OrdinalIgnoreCase);
            remaining = remaining.Replace("hazirlik", string.Empty, StringComparison.OrdinalIgnoreCase);
            remaining = remaining.Replace("ilkisinma", string.Empty, StringComparison.OrdinalIgnoreCase);
            remaining = remaining.Replace("isinma", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(remaining))
            {
                return "Temizlik/Hazırlık/İlk Isınma";
            }

            return trimmed;
        }

        var viewModel = new PvcDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.PvcRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<PvcDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        string? NormalizeMachine(string? name) => string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        bool IsSameMachine(string? source, string target) =>
            string.Equals(NormalizeMachine(source), target, StringComparison.OrdinalIgnoreCase);

        var seciliMakine = NormalizeMachine(makine);
        var seciliMakineSatirlari = string.IsNullOrWhiteSpace(seciliMakine)
            ? new List<PvcSatirModel>()
            : excelData.Where(x => IsSameMachine(x.Makine, seciliMakine!)).ToList();
        if (!string.IsNullOrWhiteSpace(seciliMakine) && !seciliMakineSatirlari.Any())
        {
            seciliMakine = null;
        }

        var tarihKaynak = string.IsNullOrWhiteSpace(seciliMakine) ? excelData : seciliMakineSatirlari;
        AddFilterAvailabilityMetadata(bag, tarihKaynak.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(tarihKaynak.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        viewModel.SeciliMakine = seciliMakine;

        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var periodVeriTumMakineler = excelData.AsQueryable();
        if (hasDateRange)
        {
            var start = rangeStart!.Value;
            var end = rangeEnd!.Value;
            periodVeriTumMakineler = periodVeriTumMakineler.Where(x => x.Tarih.Date >= start && x.Tarih.Date <= end);
            bag["PvcRange"] = $"{start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            bag["UretimTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
            bag["PvcOeeTitle"] = "OEE Skoru Trendi (Tarih Aralığı)";
            bag["PvcMakineOeeTitle"] = "Makine Bazlı OEE Karşılaştırma (Tarih Aralığı Ort.)";
            bag["KayipSureTrendTitle"] = "Kayıp Süre (Tarih Aralığı)";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            var ayValue = ay.Value;
            var yilValue = yil.Value;
            var ayBaslangic = new DateTime(yilValue, ayValue, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);
            periodVeriTumMakineler = periodVeriTumMakineler.Where(x => x.Tarih.Month == ayValue && x.Tarih.Year == yilValue);
            bag["PvcRange"] = $"{ayBaslangic:dd.MM.yyyy} - {ayBitis:dd.MM.yyyy}";
            bag["UretimTrendTitle"] = "Üretim Trendi (Aylık)";
            bag["PvcOeeTitle"] = "OEE Skoru Trendi (Aylık)";
            bag["PvcMakineOeeTitle"] = "Makine Bazlı OEE Karşılaştırma (Aylık Ort.)";
            bag["KayipSureTrendTitle"] = "Kayıp Süre (Aylık)";
        }
        else
        {
            periodVeriTumMakineler = periodVeriTumMakineler.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["PvcRange"] = $"{islenecekTarih:dd.MM.yyyy}";
            bag["UretimTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
            bag["PvcOeeTitle"] = "OEE Skoru Trendi (Son 7 Gün)";
            bag["PvcMakineOeeTitle"] = $"Makine Bazlı OEE Karşılaştırma ({islenecekTarih:dd.MM.yyyy})";
            bag["KayipSureTrendTitle"] = "Kayıp Süre (Son 7 Gün)";
        }

        var periodVeriQuery = periodVeriTumMakineler;
        if (!string.IsNullOrWhiteSpace(seciliMakine))
        {
            var seciliMakineKey = DashboardParsingHelper.NormalizeLabel(seciliMakine);
            periodVeriQuery = periodVeriQuery.Where(x => DashboardParsingHelper.NormalizeLabel(x.Makine) == seciliMakineKey);
        }

        var filtreliVeri = periodVeriQuery.ToList();
        var periodVeriAllMachinesList = periodVeriTumMakineler.ToList();
        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filtreliVeri.Select(x => x.Tarih));
        var pvcPersonelStart = hasDateRange
            ? rangeStart!.Value
            : ay.HasValue && yil.HasValue
                ? new DateTime(yil.Value, ay.Value, 1)
                : islenecekTarih.Date;
        var pvcPersonelEnd = hasDateRange
            ? rangeEnd!.Value
            : ay.HasValue && yil.HasValue
                ? new DateTime(yil.Value, ay.Value, 1).AddMonths(1).AddDays(-1)
                : islenecekTarih.Date;
        viewModel.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, pvcPersonelStart, pvcPersonelEnd, IsPvcDepartment);

        viewModel.ToplamUretimMetraj = filtreliVeri.Sum(x => x.UretimMetraj);
        viewModel.ToplamParcaSayisi = filtreliVeri.Sum(x => x.ParcaSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3);
        viewModel.OrtalamaFiiliCalismaOrani = filtreliVeri.Any() ? filtreliVeri.Average(x => x.FiiliCalismaOrani) : 0;
        viewModel.OrtalamaPerformans = filtreliVeri.Select(x => x.Performans).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKullanilabilirlik = filtreliVeri.Select(x => x.Kullanilabilirlik).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKalite = filtreliVeri.Select(x => x.Kalite).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaOee = filtreliVeri.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var trendKaynak = string.IsNullOrWhiteSpace(seciliMakine)
            ? excelData
            : excelData.Where(x => IsSameMachine(x.Makine, seciliMakine!)).ToList();

        var uretimGunluk = trendKaynak
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.UretimMetraj));

        viewModel.UretimTrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.UretimTrendData = tumTarihler.Select(t => uretimGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var performansGunluk = trendKaynak
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Performans > 0).Select(x => x.Performans).DefaultIfEmpty(0).Average());

        viewModel.FiiliCalismaLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.FiiliCalismaData = tumTarihler.Select(t => performansGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.UretimOraniTrendData = viewModel.FiiliCalismaData.ToList();

        var kayipGunluk = trendKaynak
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.KayipSure > 0).Select(x => x.KayipSure).DefaultIfEmpty(0).Average());

        viewModel.KayipSureData = tumTarihler.Select(t => kayipGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var oeeGunluk = trendKaynak
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());

        viewModel.OeeTrendData = tumTarihler.Select(t => oeeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var makineOeeGruplari = filtreliVeri
            .GroupBy(
                x => string.IsNullOrWhiteSpace(x.Makine) ? "Bilinmeyen" : x.Makine.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Makine = g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Makine))?.Makine?.Trim() ?? "Bilinmeyen",
                Satirlar = g.ToList()
            })
            .OrderBy(x => x.Makine)
            .ToList();
        viewModel.MakineOeeSerieLabels = makineOeeGruplari.Select(x => x.Makine).ToList();
        viewModel.MakineOeeTrendSeries = makineOeeGruplari
            .Select(group => tumTarihler
                .Select(t => group.Satirlar
                    .Where(x => x.Tarih.Date == t && x.Oee > 0)
                    .Select(x => x.Oee)
                    .DefaultIfEmpty(0)
                    .Average())
                .ToList())
            .ToList();

        var makineGruplari = filtreliVeri
            .GroupBy(x => x.Makine ?? "Bilinmeyen")
            .Select(g => new { Makine = g.Key, Metraj = g.Sum(x => x.UretimMetraj), Parca = g.Sum(x => x.ParcaSayisi) })
            .OrderByDescending(x => x.Metraj)
            .ToList();

        viewModel.MakineLabels = makineGruplari.Select(x => x.Makine).ToList();
        viewModel.MakineUretimData = makineGruplari.Select(x => x.Metraj).ToList();
        viewModel.MakineParcaData = makineGruplari.Select(x => x.Parca).ToList();

        viewModel.MakineKartlari = periodVeriAllMachinesList
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Makine) ? "Bilinmeyen" : x.Makine.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new MakineKartOzetModel
            {
                MakineAdi = g.Key,
                Uretim = g.Sum(x => x.UretimMetraj),
                HataliParca = g.Sum(x => x.HataliParca),
                DuraklamaDakika = g.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3),
                Oee = g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average()
            })
            .OrderByDescending(x => x.Uretim)
            .ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in filtreliVeri)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, NormalizePvcDuraklamaNedeni(row.DuraklamaNedeni1), row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, NormalizePvcDuraklamaNedeni(row.DuraklamaNedeni2), row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, NormalizePvcDuraklamaNedeni(row.DuraklamaNedeni3), row.Duraklama3);
        }

        var duraklamaList = duraklamaNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        return new DashboardPageResult<PvcDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<CncDashboardViewModel>> GetCncAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();
        var personelRows = NormalizePersonelRows(snapshot.PersonelRows);

        var viewModel = new CncDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var masterRows = snapshot.MasterwoodRows.Where(x => x.Tarih != DateTime.MinValue).ToList();
        var skipperRows = snapshot.SkipperRows.Where(x => x.Tarih != DateTime.MinValue).ToList();
        var roverRows = snapshot.RoverBRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!masterRows.Any() && !skipperRows.Any() && !roverRows.Any())
        {
            bag["ErrorMessage"] = "CNC verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<CncDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        var allDates = masterRows.Select(x => x.Tarih.Date)
            .Concat(skipperRows.Select(x => x.Tarih.Date))
            .Concat(roverRows.Select(x => x.Tarih.Date))
            .ToList();
        AddFilterAvailabilityMetadata(bag, allDates);
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(allDates, DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var maxDate = allDates.Any() ? allDates.Max() : DateTime.Today;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        DateTime periodStart;
        DateTime periodEnd;
        DateTime trendStart;
        DateTime trendEnd;
        if (hasDateRange)
        {
            periodStart = rangeStart!.Value;
            periodEnd = rangeEnd!.Value;
            trendStart = periodStart;
            trendEnd = periodEnd;
            bag["CncRange"] = $"{periodStart:dd.MM.yyyy} - {periodEnd:dd.MM.yyyy}";
            bag["CncUretimTrendTitle"] = "CNC Üretim Trendi (Tarih Aralığı)";
            bag["CncHataliTrendTitle"] = "CNC Hatalı Parça Trendi (Tarih Aralığı)";
            bag["CncOeeTrendTitle"] = "CNC OEE Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue)
        {
            var resolvedYear = DashboardParsingHelper.ResolveYearForMonth(allDates, ay.Value, yil);
            var yearToUse = resolvedYear ?? yil ?? maxDate.Year;
            if (resolvedYear.HasValue && (!yil.HasValue || yil.Value != resolvedYear.Value))
            {
                bag["CncResolvedYear"] = resolvedYear.Value;
            }

            periodStart = new DateTime(yearToUse, ay.Value, 1);
            periodEnd = periodStart.AddMonths(1).AddDays(-1);
            trendStart = periodStart;
            trendEnd = periodEnd;
            bag["CncRange"] = $"{periodStart:dd.MM.yyyy} - {periodEnd:dd.MM.yyyy}";
            bag["CncUretimTrendTitle"] = "CNC Üretim Trendi (Aylık)";
            bag["CncHataliTrendTitle"] = "CNC Hatalı Parça Trendi (Aylık)";
            bag["CncOeeTrendTitle"] = "CNC OEE Trendi (Aylık)";
        }
        else
        {
            periodStart = islenecekTarih.Date;
            periodEnd = islenecekTarih.Date;
            trendStart = islenecekTarih.Date.AddDays(-6);
            trendEnd = islenecekTarih.Date;
            bag["CncRange"] = $"{periodStart:dd.MM.yyyy}";
            bag["CncUretimTrendTitle"] = "CNC Üretim Trendi (Son 7 Gün)";
            bag["CncHataliTrendTitle"] = "CNC Hatalı Parça Trendi (Son 7 Gün)";
            bag["CncOeeTrendTitle"] = "CNC OEE Trendi (Son 7 Gün)";
        }

        var masterPeriod = masterRows.Where(x => x.Tarih.Date >= periodStart && x.Tarih.Date <= periodEnd).ToList();
        var skipperPeriod = skipperRows.Where(x => x.Tarih.Date >= periodStart && x.Tarih.Date <= periodEnd).ToList();
        var roverPeriod = roverRows.Where(x => x.Tarih.Date >= periodStart && x.Tarih.Date <= periodEnd).ToList();
        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(
            masterPeriod.Select(x => x.Tarih)
                .Concat(skipperPeriod.Select(x => x.Tarih))
                .Concat(roverPeriod.Select(x => x.Tarih)));
        viewModel.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, periodStart, periodEnd, IsCncDepartment);

        viewModel.Masterwood = new CncMachineSummary
        {
            Uretim = masterPeriod.Sum(x => x.DelikFreezeSayisi),
            HataliParca = masterPeriod.Sum(x => x.HataliParca),
            DuraklamaDakika = masterPeriod.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3),
            Oee = masterPeriod.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average()
        };
        viewModel.Skipper = new CncMachineSummary
        {
            Uretim = skipperPeriod.Sum(x => x.DelikSayisi),
            HataliParca = skipperPeriod.Sum(x => x.HataliParca),
            DuraklamaDakika = skipperPeriod.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3),
            Oee = skipperPeriod.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average()
        };
        viewModel.RoverB = new CncMachineSummary
        {
            Uretim = roverPeriod.Sum(x => x.DelikFreezePvcSayisi),
            HataliParca = roverPeriod.Sum(x => x.HataliParca),
            DuraklamaDakika = roverPeriod.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3 + x.Duraklama4),
            Oee = roverPeriod.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average()
        };

        viewModel.ToplamUretim = viewModel.Masterwood.Uretim + viewModel.Skipper.Uretim + viewModel.RoverB.Uretim;
        viewModel.ToplamHataliParca = viewModel.Masterwood.HataliParca + viewModel.Skipper.HataliParca + viewModel.RoverB.HataliParca;
        viewModel.ToplamDuraklamaDakika = viewModel.Masterwood.DuraklamaDakika + viewModel.Skipper.DuraklamaDakika + viewModel.RoverB.DuraklamaDakika;

        var perfValues = masterPeriod.Select(x => x.Performans)
            .Concat(skipperPeriod.Select(x => x.Performans))
            .Concat(roverPeriod.Select(x => x.Performans))
            .Where(x => x > 0)
            .ToList();
        viewModel.OrtalamaPerformans = perfValues.Any() ? perfValues.Average() : 0;

        var oeeValues = masterPeriod.Select(x => x.Oee)
            .Concat(skipperPeriod.Select(x => x.Oee))
            .Concat(roverPeriod.Select(x => x.Oee))
            .Where(x => x > 0)
            .ToList();
        viewModel.OrtalamaOee = oeeValues.Any() ? oeeValues.Average() : 0;

        var trendDates = Enumerable.Range(0, (trendEnd.Date - trendStart.Date).Days + 1)
            .Select(offset => trendStart.Date.AddDays(offset))
            .ToList();
        viewModel.TrendLabels = trendDates.Select(t => t.ToString("dd.MM")).ToList();

        var masterTrendUretim = masterRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikFreezeSayisi));
        var skipperTrendUretim = skipperRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikSayisi));
        var roverTrendUretim = roverRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikFreezePvcSayisi));
        var masterTrendHatali = masterRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.HataliParca));
        var skipperTrendHatali = skipperRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.HataliParca));
        var roverTrendHatali = roverRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.HataliParca));

        viewModel.UretimTrendData = trendDates.Select(t =>
            (masterTrendUretim.TryGetValue(t, out var m) ? m : 0)
            + (skipperTrendUretim.TryGetValue(t, out var s) ? s : 0)
            + (roverTrendUretim.TryGetValue(t, out var r) ? r : 0)
        ).ToList();
        viewModel.HataliParcaTrendData = trendDates.Select(t =>
            (masterTrendHatali.TryGetValue(t, out var m) ? m : 0)
            + (skipperTrendHatali.TryGetValue(t, out var s) ? s : 0)
            + (roverTrendHatali.TryGetValue(t, out var r) ? r : 0)
        ).ToList();

        var masterTrendOee = masterRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());
        var skipperTrendOee = skipperRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());
        var roverTrendOee = roverRows
            .Where(x => x.Tarih.Date >= trendStart && x.Tarih.Date <= trendEnd)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());

        viewModel.OeeTrendData = trendDates.Select(t =>
        {
            var vals = new List<double>();
            if (masterTrendOee.TryGetValue(t, out var m) && m > 0) vals.Add(m);
            if (skipperTrendOee.TryGetValue(t, out var s) && s > 0) vals.Add(s);
            if (roverTrendOee.TryGetValue(t, out var r) && r > 0) vals.Add(r);
            return vals.Any() ? vals.Average() : 0;
        }).ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in masterPeriod)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
        }

        foreach (var row in skipperPeriod)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
        }

        foreach (var row in roverPeriod)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni4, row.Duraklama4);
        }

        var duraklamaList = duraklamaNedenleri
            .OrderByDescending(x => x.Value)
            .ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        return new DashboardPageResult<CncDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<MasterwoodDashboardViewModel>> GetMasterwoodAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();

        var viewModel = new MasterwoodDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.MasterwoodRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<MasterwoodDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        AddFilterAvailabilityMetadata(bag, excelData.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliVeri = excelData.AsQueryable();
        if (hasDateRange)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            bag["MasterwoodTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
            bag["MasterwoodHataliTrendTitle"] = "Hatalı Parça Trendi (Tarih Aralığı)";
            bag["KisiTrendTitle"] = "Kişi Sayısı (Tarih Aralığı)";
            bag["MasterwoodOeeTitle"] = "OEE Skoru Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
            bag["MasterwoodTrendTitle"] = "Üretim Trendi (Aylık)";
            bag["MasterwoodHataliTrendTitle"] = "Hatalı Parça Trendi (Aylık)";
            bag["KisiTrendTitle"] = "Kişi Sayısı (Aylık)";
            bag["MasterwoodOeeTitle"] = "OEE Skoru Trendi (Aylık)";
        }
        else
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["MasterwoodTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
            bag["MasterwoodHataliTrendTitle"] = "Hatalı Parça Trendi (Son 7 Gün)";
            bag["KisiTrendTitle"] = "Kişi Sayısı (Son 7 Gün)";
            bag["MasterwoodOeeTitle"] = "OEE Skoru Trendi (Son 7 Gün)";
        }

        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filtreliVeri.Select(x => x.Tarih));
        viewModel.ToplamDelik = filtreliVeri.Sum(x => x.DelikSayisi);
        viewModel.ToplamDelikFreeze = filtreliVeri.Sum(x => x.DelikFreezeSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
        viewModel.OrtalamaCalisanPersonel = RoundPersonnelAverage(viewModel.OrtalamaKisiSayisi);
        viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3);
        viewModel.OrtalamaPerformans = filtreliVeri.Select(x => x.Performans).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKullanilabilirlik = filtreliVeri.Select(x => x.Kullanilabilirlik).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKalite = filtreliVeri.Select(x => x.Kalite).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaOee = filtreliVeri.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var delikGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikSayisi));
        var delikFreezeGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikFreezeSayisi));
        var hataliParcaGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.HataliParca));
        var kisiGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.KisiSayisi));

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.DelikTrendData = tumTarihler.Select(t => delikGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.DelikFreezeTrendData = tumTarihler.Select(t => delikFreezeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.HataliParcaTrendData = tumTarihler.Select(t => hataliParcaGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KisiTrendData = tumTarihler.Select(t => kisiGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var performansGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Performans > 0).Select(x => x.Performans).DefaultIfEmpty(0).Average());
        var kayipSureGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.KayipSureOrani > 0).Select(x => x.KayipSureOrani).DefaultIfEmpty(0).Average());
        var oeeGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());

        viewModel.UretimOraniTrendData = tumTarihler.Select(t => performansGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KayipSureTrendData = tumTarihler.Select(t => kayipSureGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.OeeTrendData = tumTarihler.Select(t => oeeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var kosulList = filtreliVeri
            .GroupBy(x => x.CalismaKosulu ?? "Bilinmeyen")
            .Select(g => new { Kosul = g.Key, Toplam = g.Sum(x => x.DelikFreezeSayisi) })
            .OrderByDescending(x => x.Toplam)
            .ToList();

        viewModel.CalismaKosuluLabels = kosulList.Select(x => x.Kosul).ToList();
        viewModel.CalismaKosuluData = kosulList.Select(x => x.Toplam).ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in filtreliVeri)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
        }

        var duraklamaList = duraklamaNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        return new DashboardPageResult<MasterwoodDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<SkipperDashboardViewModel>> GetSkipperAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();

        var viewModel = new SkipperDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.SkipperRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<SkipperDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        AddFilterAvailabilityMetadata(bag, excelData.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliVeri = excelData.AsQueryable();
        if (hasDateRange)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            bag["SkipperTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
            bag["SkipperHataliTrendTitle"] = "Hatalı Parça Trendi (Tarih Aralığı)";
            bag["SkipperKisiTrendTitle"] = "Kişi Sayısı (Tarih Aralığı)";
            bag["SkipperOeeTitle"] = "OEE Skoru Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
            bag["SkipperTrendTitle"] = "Üretim Trendi (Aylık)";
            bag["SkipperHataliTrendTitle"] = "Hatalı Parça Trendi (Aylık)";
            bag["SkipperKisiTrendTitle"] = "Kişi Sayısı (Aylık)";
            bag["SkipperOeeTitle"] = "OEE Skoru Trendi (Aylık)";
        }
        else
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["SkipperTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
            bag["SkipperHataliTrendTitle"] = "Hatalı Parça Trendi (Son 7 Gün)";
            bag["SkipperKisiTrendTitle"] = "Kişi Sayısı (Son 7 Gün)";
            bag["SkipperOeeTitle"] = "OEE Skoru Trendi (Son 7 Gün)";
        }

        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filtreliVeri.Select(x => x.Tarih));
        viewModel.ToplamDelik = filtreliVeri.Sum(x => x.DelikSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
        viewModel.OrtalamaCalisanPersonel = RoundPersonnelAverage(viewModel.OrtalamaKisiSayisi);
        viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3);
        viewModel.OrtalamaFiiliCalismaOrani = filtreliVeri.Any() ? filtreliVeri.Average(x => x.FiiliCalismaOrani) : 0;
        viewModel.OrtalamaPerformans = filtreliVeri.Select(x => x.Performans).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKullanilabilirlik = filtreliVeri.Select(x => x.Kullanilabilirlik).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKalite = filtreliVeri.Select(x => x.Kalite).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaOee = filtreliVeri.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var delikGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikSayisi));
        var hataliParcaGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.HataliParca));
        var kisiGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.KisiSayisi));

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.DelikTrendData = tumTarihler.Select(t => delikGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.HataliParcaTrendData = tumTarihler.Select(t => hataliParcaGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KisiTrendData = tumTarihler.Select(t => kisiGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var performansGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Performans > 0).Select(x => x.Performans).DefaultIfEmpty(0).Average());
        var kayipSureGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.KayipSureOrani > 0).Select(x => x.KayipSureOrani).DefaultIfEmpty(0).Average());
        var oeeGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());

        viewModel.UretimOraniTrendData = tumTarihler.Select(t => performansGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KayipSureTrendData = tumTarihler.Select(t => kayipSureGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.OeeTrendData = tumTarihler.Select(t => oeeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in filtreliVeri)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
        }

        var duraklamaList = duraklamaNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        return new DashboardPageResult<SkipperDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<RoverBDashboardViewModel>> GetRoverBAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();

        var viewModel = new RoverBDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.RoverBRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<RoverBDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        AddFilterAvailabilityMetadata(bag, excelData.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliVeri = excelData.AsQueryable();
        if (hasDateRange)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            bag["RoverBTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
            bag["RoverBHataliTrendTitle"] = "Hatalı Parça Trendi (Tarih Aralığı)";
            bag["RoverBOeeTitle"] = "OEE Skoru Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
            bag["RoverBTrendTitle"] = "Üretim Trendi (Aylık)";
            bag["RoverBHataliTrendTitle"] = "Hatalı Parça Trendi (Aylık)";
            bag["RoverBOeeTitle"] = "OEE Skoru Trendi (Aylık)";
        }
        else
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["RoverBTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
            bag["RoverBHataliTrendTitle"] = "Hatalı Parça Trendi (Son 7 Gün)";
            bag["RoverBOeeTitle"] = "OEE Skoru Trendi (Son 7 Gün)";
        }

        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filtreliVeri.Select(x => x.Tarih));
        viewModel.ToplamDelikFreeze = filtreliVeri.Sum(x => x.DelikFreezeSayisi);
        viewModel.ToplamDelikFreezePvc = filtreliVeri.Sum(x => x.DelikFreezePvcSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
        viewModel.OrtalamaCalisanPersonel = RoundPersonnelAverage(viewModel.OrtalamaKisiSayisi);
        viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3 + x.Duraklama4);
        viewModel.OrtalamaPerformans = filtreliVeri.Select(x => x.Performans).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKullanilabilirlik = filtreliVeri.Select(x => x.Kullanilabilirlik).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaKalite = filtreliVeri.Select(x => x.Kalite).Where(x => x > 0).DefaultIfEmpty(0).Average();
        viewModel.OrtalamaOee = filtreliVeri.Select(x => x.Oee).Where(x => x > 0).DefaultIfEmpty(0).Average();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var delikFreezeGunluk = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikFreezeSayisi));
        var delikFreezePvcGunluk = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikFreezePvcSayisi));
        var hataliParcaGunluk = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.HataliParca));

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.DelikFreezeTrendData = tumTarihler.Select(t => delikFreezeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.DelikFreezePvcTrendData = tumTarihler.Select(t => delikFreezePvcGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.HataliParcaTrendData = tumTarihler.Select(t => hataliParcaGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var performansGunluk = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Performans > 0).Select(x => x.Performans).DefaultIfEmpty(0).Average());
        var kayipSureGunluk = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.KayipSureOrani > 0).Select(x => x.KayipSureOrani).DefaultIfEmpty(0).Average());
        var oeeGunluk = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());

        viewModel.UretimOraniTrendData = tumTarihler.Select(t => performansGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KayipSureTrendData = tumTarihler.Select(t => kayipSureGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.OeeTrendData = tumTarihler.Select(t => oeeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in filtreliVeri)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni4, row.Duraklama4);
        }

        var duraklamaList = duraklamaNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        return new DashboardPageResult<RoverBDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<TezgahDashboardViewModel>> GetTezgahAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();

        var viewModel = new TezgahDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.TezgahRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<TezgahDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        AddFilterAvailabilityMetadata(bag, excelData.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliVeri = excelData.AsEnumerable();
        int? yearToUse = null;
        if (hasDateRange)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            bag["TezgahTrendTitle"] = "Parça ve Kayıp Süre Trendi (Tarih Aralığı)";
            bag["TezgahOeeTitle"] = "OEE, Performans ve Kalite Trendi (Tarih Aralığı)";
            bag["TezgahKosulTitle"] = "Çalışma Koşulu Kırılımı (Tarih Aralığı)";
        }
        else if (ay.HasValue)
        {
            var resolvedYear = DashboardParsingHelper.ResolveYearForMonth(excelData.Select(x => x.Tarih), ay.Value, yil);
            yearToUse = resolvedYear ?? yil ?? islenecekTarih.Year;
            if (resolvedYear.HasValue && (!yil.HasValue || yil.Value != resolvedYear.Value))
            {
                bag["TezgahResolvedYear"] = resolvedYear.Value;
            }

            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yearToUse.Value);
            bag["TezgahTrendTitle"] = "Parça ve Kayıp Süre Trendi (Aylık)";
            bag["TezgahOeeTitle"] = "OEE, Performans ve Kalite Trendi (Aylık)";
            bag["TezgahKosulTitle"] = "Çalışma Koşulu Kırılımı (Aylık)";
        }
        else
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["TezgahTrendTitle"] = "Parça ve Kayıp Süre Trendi (Son 7 Gün)";
            bag["TezgahOeeTitle"] = "OEE, Performans ve Kalite Trendi (Son 7 Gün)";
            bag["TezgahKosulTitle"] = $"Çalışma Koşulu Kırılımı ({islenecekTarih:dd.MM.yyyy})";
        }

        var seciliVeri = filtreliVeri.ToList();
        static double AveragePositive(IEnumerable<double> values)
        {
            var filtered = values.Where(x => x > 0).ToList();
            return filtered.Any() ? filtered.Average() : 0;
        }

        static double MaxPositive(IEnumerable<double> values)
        {
            var filtered = values.Where(x => x > 0).ToList();
            return filtered.Any() ? filtered.Max() : 0;
        }

        var gunlukOzetler = seciliVeri
            .GroupBy(x => x.Tarih.Date)
            .Select(g => new
            {
                Tarih = g.Key,
                ToplamParcaAdeti = g.Sum(x => x.ParcaAdeti),
                ToplamKayipSureDakika = g.Sum(x => x.KayipSureDakika),
                ToplamHataliParcaSayisi = g.Sum(x => x.HataliParcaSayisi),
                ToplamYapilmasiGerekenSure = g.Sum(x => x.YapilmasiGerekenSure > 0 ? x.YapilmasiGerekenSure : x.SureDakika),
                ToplamAdamSaat = MaxPositive(g.Select(x => x.ToplamAdamSaat)),
                ToplamKalanAdamSaat = g.Sum(x => x.KalanAdamSaat),
                GunlukKapasite = MaxPositive(g.Select(x => x.GunlukKapasite)),
                BugunCalisanToplamKisiSayisi = MaxPositive(g.Select(x => x.BugunCalisanToplamKisiSayisi > 0 ? x.BugunCalisanToplamKisiSayisi : x.KisiSayisi)),
                Performans = AveragePositive(g.Select(x => x.Performans)),
                Kullanilabilirlik = AveragePositive(g.Select(x => x.Kullanilabilirlik)),
                Kalite = AveragePositive(g.Select(x => x.Kalite)),
                Oee = AveragePositive(g.Select(x => x.Oee))
            })
            .OrderBy(x => x.Tarih)
            .ToList();

        viewModel.CalisilanIsGunu = gunlukOzetler.Count;
        viewModel.ToplamParcaAdeti = gunlukOzetler.Sum(x => x.ToplamParcaAdeti);
        viewModel.ToplamSureDakika = gunlukOzetler.Sum(x => x.ToplamYapilmasiGerekenSure);
        viewModel.ToplamKayipSureDakika = gunlukOzetler.Sum(x => x.ToplamKayipSureDakika);
        viewModel.ToplamNetSureDakika = Math.Max(0, viewModel.ToplamSureDakika - viewModel.ToplamKayipSureDakika);
        viewModel.ToplamHataliParcaSayisi = gunlukOzetler.Sum(x => x.ToplamHataliParcaSayisi);
        viewModel.ToplamAdamSaat = gunlukOzetler.Sum(x => x.ToplamAdamSaat);
        viewModel.ToplamKalanAdamSaat = gunlukOzetler.Sum(x => x.ToplamKalanAdamSaat);
        viewModel.OrtalamaGunlukKapasite = AveragePositive(gunlukOzetler.Select(x => x.GunlukKapasite));
        viewModel.OrtalamaKisiSayisi = AveragePositive(gunlukOzetler.Select(x => x.BugunCalisanToplamKisiSayisi));
        viewModel.OrtalamaCalisanPersonel = RoundPersonnelAverage(viewModel.OrtalamaKisiSayisi);
        viewModel.OrtalamaPerformans = AveragePositive(gunlukOzetler.Select(x => x.Performans));
        viewModel.OrtalamaKullanilabilirlik = AveragePositive(gunlukOzetler.Select(x => x.Kullanilabilirlik));
        viewModel.OrtalamaKalite = AveragePositive(gunlukOzetler.Select(x => x.Kalite));
        viewModel.OrtalamaVerimliCalismaOrani = viewModel.OrtalamaKullanilabilirlik;
        viewModel.OrtalamaOee = AveragePositive(gunlukOzetler.Select(x => x.Oee));
        viewModel.OrtalamaSaatlikUretim = viewModel.ToplamSureDakika > 0
            ? viewModel.ToplamParcaAdeti / (viewModel.ToplamSureDakika / 60d)
            : 0;
        viewModel.AktifUrunSayisi = seciliVeri
            .Where(x => !string.IsNullOrWhiteSpace(x.TezgahUrunleri))
            .Select(x => x.TezgahUrunleri!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        viewModel.KayitSayisi = seciliVeri.Count;

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue)
        {
            var yearForTrend = yearToUse ?? yil ?? islenecekTarih.Year;
            trendBaslangic = new DateTime(yearForTrend, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var trendVerisi = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date).ToList();

        var trendGunluk = trendVerisi
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    ToplamParcaAdeti = g.Sum(x => x.ParcaAdeti),
                    ToplamKayipSureDakika = g.Sum(x => x.KayipSureDakika),
                    Performans = AveragePositive(g.Select(x => x.Performans)),
                    Kullanilabilirlik = AveragePositive(g.Select(x => x.Kullanilabilirlik)),
                    Kalite = AveragePositive(g.Select(x => x.Kalite)),
                    Oee = AveragePositive(g.Select(x => x.Oee))
                });

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.GunlukParcaTrendData = tumTarihler.Select(t => trendGunluk.TryGetValue(t, out var v) ? v.ToplamParcaAdeti : 0).ToList();
        viewModel.GunlukKayipSureTrendData = tumTarihler.Select(t => trendGunluk.TryGetValue(t, out var v) ? v.ToplamKayipSureDakika : 0).ToList();
        viewModel.OeeTrendData = tumTarihler.Select(t => trendGunluk.TryGetValue(t, out var v) ? v.Oee : 0).ToList();
        viewModel.PerformansTrendData = tumTarihler.Select(t => trendGunluk.TryGetValue(t, out var v) ? v.Performans : 0).ToList();
        viewModel.KullanilabilirlikTrendData = tumTarihler.Select(t => trendGunluk.TryGetValue(t, out var v) ? v.Kullanilabilirlik : 0).ToList();
        viewModel.KaliteTrendData = tumTarihler.Select(t => trendGunluk.TryGetValue(t, out var v) ? v.Kalite : 0).ToList();

        var kayipNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in seciliVeri)
        {
            foreach (var kayip in row.GetKayipSureKalemleri())
            {
                DashboardParsingHelper.AddDuraklama(kayipNedenleri, kayip.Neden, kayip.Dakika);
            }
        }

        var kayipList = kayipNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.KayipNedenLabels = kayipList.Select(x => x.Key).ToList();
        viewModel.KayipNedenData = kayipList.Select(x => x.Value).ToList();
        viewModel.BaskinKayipNedeni = kayipList.FirstOrDefault().Key ?? "Kayıp girilmedi";

        var urunler = seciliVeri
            .Where(x => !string.IsNullOrWhiteSpace(x.TezgahUrunleri))
            .GroupBy(x => x.TezgahUrunleri!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Urun = g.First(x => !string.IsNullOrWhiteSpace(x.TezgahUrunleri)).TezgahUrunleri!.Trim(),
                ToplamParca = g.Sum(x => x.ParcaAdeti),
                ToplamKayipSure = g.Sum(x => x.KayipSureDakika)
            })
            .OrderByDescending(x => x.ToplamParca)
            .Take(8)
            .ToList();

        viewModel.UrunLabels = urunler.Select(x => x.Urun).ToList();
        viewModel.UrunParcaData = urunler.Select(x => x.ToplamParca).ToList();
        viewModel.UrunKayipSureData = urunler.Select(x => x.ToplamKayipSure).ToList();
        viewModel.OneCikanUrun = urunler.FirstOrDefault()?.Urun ?? "Ürün kaydı yok";

        var kosullar = seciliVeri
            .GroupBy(x => string.IsNullOrWhiteSpace(x.CalismaKosulu) ? "Belirtilmedi" : x.CalismaKosulu!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Kosul = g.Select(x => x.CalismaKosulu?.Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Belirtilmedi",
                ToplamKayipSure = g.Sum(x => x.KayipSureDakika),
                ToplamParca = g.Sum(x => x.ParcaAdeti)
            })
            .OrderByDescending(x => x.ToplamKayipSure)
            .ToList();

        viewModel.CalismaKosuluLabels = kosullar.Select(x => x.Kosul).ToList();
        viewModel.CalismaKosuluKayipSureData = kosullar.Select(x => x.ToplamKayipSure).ToList();
        viewModel.CalismaKosuluParcaData = kosullar.Select(x => x.ToplamParca).ToList();

        return new DashboardPageResult<TezgahDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<EbatlamaDashboardViewModel>> GetEbatlamaAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, string? makine, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();
        var personelRows = NormalizePersonelRows(snapshot.PersonelRows);
        const double macmazzaOtoDuraklamaDakika = 540d;
        const string macmazzaOtoDuraklamaNedeni = "Makine çalışmadı";

        static bool IsMacmazzaMachine(string? makine)
        {
            if (string.IsNullOrWhiteSpace(makine))
            {
                return false;
            }

            return makine.Trim().Contains("macmazza", StringComparison.OrdinalIgnoreCase);
        }

        var viewModel = new EbatlamaDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.EbatlamaRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<EbatlamaDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        string? NormalizeMachine(string? name) => string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        bool IsSameMachine(string? source, string target) =>
            string.Equals(NormalizeMachine(source), target, StringComparison.OrdinalIgnoreCase);
        static double GetToplamKesimAdet(EbatlamaSatirModel row) => row.Plaka8Mm + row.Plaka18Mm + row.Plaka30Mm;

        var seciliMakine = NormalizeMachine(makine);
        var seciliMakineSatirlari = string.IsNullOrWhiteSpace(seciliMakine)
            ? new List<EbatlamaSatirModel>()
            : excelData.Where(x => IsSameMachine(x.Makine, seciliMakine!)).ToList();
        if (!string.IsNullOrWhiteSpace(seciliMakine) && !seciliMakineSatirlari.Any())
        {
            seciliMakine = null;
        }

        var tarihKaynak = string.IsNullOrWhiteSpace(seciliMakine) ? excelData : seciliMakineSatirlari;
        AddFilterAvailabilityMetadata(bag, tarihKaynak.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(tarihKaynak.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        viewModel.SeciliMakine = seciliMakine;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;
        var isSingleDayRange = hasDateRange && rangeStart!.Value.Date == rangeEnd!.Value.Date;

        var periodVeriTumMakineler = excelData.AsQueryable();
        int? yearToUse = null;
        if (hasDateRange)
        {
            var start = rangeStart!.Value;
            var end = rangeEnd!.Value;
            periodVeriTumMakineler = periodVeriTumMakineler.Where(x => x.Tarih.Date >= start && x.Tarih.Date <= end);
            if (isSingleDayRange)
            {
                bag["EbatlamaRange"] = $"{start:dd.MM.yyyy}";
                bag["EbatlamaTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
                bag["EbatlamaPlakaTrendTitle"] = "Plaka Trendleri (Son 7 Gün)";
                bag["EbatlamaGonyTitle"] = "Gönyelleme (Son 7 Gün)";
                bag["EbatlamaOeeTitle"] = "OEE Skoru Trendi (Son 7 Gün)";
            }
            else
            {
                bag["EbatlamaRange"] = $"{start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
                bag["EbatlamaTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
                bag["EbatlamaPlakaTrendTitle"] = "Plaka Trendleri (Tarih Aralığı)";
                bag["EbatlamaGonyTitle"] = "Gönyelleme (Tarih Aralığı)";
                bag["EbatlamaOeeTitle"] = "OEE Skoru Trendi (Tarih Aralığı)";
            }
        }
        else if (ay.HasValue)
        {
            var resolvedYear = DashboardParsingHelper.ResolveYearForMonth(excelData.Select(x => x.Tarih), ay.Value, yil);
            yearToUse = resolvedYear ?? yil ?? islenecekTarih.Year;
            if (resolvedYear.HasValue && (!yil.HasValue || yil.Value != resolvedYear.Value))
            {
                bag["EbatlamaResolvedYear"] = resolvedYear.Value;
            }

            periodVeriTumMakineler = periodVeriTumMakineler.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yearToUse.Value);
            var ayBaslangic = new DateTime(yearToUse.Value, ay.Value, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);
            bag["EbatlamaRange"] = $"{ayBaslangic:dd.MM.yyyy} - {ayBitis:dd.MM.yyyy}";
            bag["EbatlamaTrendTitle"] = "Üretim Trendi (Aylık)";
            bag["EbatlamaPlakaTrendTitle"] = "Plaka Trendleri (Aylık)";
            bag["EbatlamaGonyTitle"] = "Gönyelleme (Aylık)";
            bag["EbatlamaOeeTitle"] = "OEE Skoru Trendi (Aylık)";
        }
        else
        {
            periodVeriTumMakineler = periodVeriTumMakineler.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["EbatlamaRange"] = $"{islenecekTarih:dd.MM.yyyy}";
            bag["EbatlamaTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
            bag["EbatlamaPlakaTrendTitle"] = "Plaka Trendleri (Son 7 Gün)";
            bag["EbatlamaGonyTitle"] = "Gönyelleme (Son 7 Gün)";
            bag["EbatlamaOeeTitle"] = "OEE Skoru Trendi (Son 7 Gün)";
        }

        var filtreliVeriQuery = periodVeriTumMakineler;
        if (!string.IsNullOrWhiteSpace(seciliMakine))
        {
            var seciliMakineKey = DashboardParsingHelper.NormalizeLabel(seciliMakine);
            filtreliVeriQuery = filtreliVeriQuery.Where(x => DashboardParsingHelper.NormalizeLabel(x.Makine) == seciliMakineKey);
        }

        var filtreliVeri = filtreliVeriQuery.ToList();
        var periodVeriAllMachinesList = periodVeriTumMakineler.ToList();
        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filtreliVeri.Select(x => x.Tarih));
        var ebatlamaPersonelStart = hasDateRange
            ? rangeStart!.Value
            : ay.HasValue
                ? new DateTime(yearToUse ?? yil ?? islenecekTarih.Year, ay.Value, 1)
                : (isSingleDayRange ? rangeStart!.Value.Date : islenecekTarih.Date);
        var ebatlamaPersonelEnd = hasDateRange
            ? rangeEnd!.Value
            : ay.HasValue
                ? new DateTime(yearToUse ?? yil ?? islenecekTarih.Year, ay.Value, 1).AddMonths(1).AddDays(-1)
                : (isSingleDayRange ? rangeEnd!.Value.Date : islenecekTarih.Date);
        viewModel.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, ebatlamaPersonelStart, ebatlamaPersonelEnd, IsEbatlamaDepartment);

        var seciliMakineMacmazza = IsMacmazzaMachine(seciliMakine);
        var macmazzaKaydiVarTumMakineler = periodVeriAllMachinesList.Any(x => IsMacmazzaMachine(x.Makine));
        var tekGunModu = !ay.HasValue && (!hasDateRange || isSingleDayRange);
        var macmazzaTekGunEksik = tekGunModu && !macmazzaKaydiVarTumMakineler;
        var macmazzaOtoDuraklamaEkle = macmazzaTekGunEksik && (string.IsNullOrWhiteSpace(seciliMakine) || seciliMakineMacmazza);
        var macmazzaEkDuraklama = macmazzaOtoDuraklamaEkle ? macmazzaOtoDuraklamaDakika : 0d;

        viewModel.ToplamKesimAdet = filtreliVeri.Sum(GetToplamKesimAdet);
        viewModel.ToplamPlaka8Mm = filtreliVeri.Sum(x => x.Plaka8Mm);
        viewModel.ToplamPlaka18Mm = filtreliVeri.Sum(x => x.Plaka18Mm);
        viewModel.ToplamPlaka30Mm = filtreliVeri.Sum(x => x.Plaka30Mm);
        viewModel.ToplamGonyelleme = filtreliVeri.Sum(x => x.Gonyelleme);
        viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2) + macmazzaEkDuraklama;
        viewModel.OrtalamaPerformans = filtreliVeri
            .Select(x => x.Performans)
            .Where(x => x > 0)
            .DefaultIfEmpty(0)
            .Average();
        viewModel.OrtalamaKullanilabilirlik = filtreliVeri
            .Select(x => x.Kullanilabilirlik)
            .Where(x => x > 0)
            .DefaultIfEmpty(0)
            .Average();
        viewModel.OrtalamaKalite = filtreliVeri
            .Select(x => x.Kalite)
            .Where(x => x > 0)
            .DefaultIfEmpty(0)
            .Average();
        viewModel.OrtalamaOee = filtreliVeri
            .Select(x => x.Oee)
            .Where(x => x > 0)
            .DefaultIfEmpty(0)
            .Average();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange && !isSingleDayRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue)
        {
            var yearForTrend = yearToUse ?? yil ?? islenecekTarih.Year;
            trendBaslangic = new DateTime(yearForTrend, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = isSingleDayRange ? rangeStart!.Value : islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var trendKaynak = string.IsNullOrWhiteSpace(seciliMakine)
            ? excelData
            : excelData.Where(x => IsSameMachine(x.Makine, seciliMakine!)).ToList();

        var kesimGunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(GetToplamKesimAdet));
        var plaka8Gunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Plaka8Mm));
        var plaka18Gunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Plaka18Mm));
        var plaka30Gunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Plaka30Mm));
        var kesim8Gunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Kesim8MmAdet));
        var kesim30Gunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Kesim30MmAdet));
        var gonyGunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Gonyelleme));
        var oeeGunluk = trendKaynak.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average());

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.KesimTrendData = tumTarihler.Select(t => kesimGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.Plaka8TrendData = tumTarihler.Select(t => plaka8Gunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.Plaka18TrendData = tumTarihler.Select(t => plaka18Gunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.Plaka30TrendData = tumTarihler.Select(t => plaka30Gunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.Kesim8TrendData = tumTarihler.Select(t => kesim8Gunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.Kesim30TrendData = tumTarihler.Select(t => kesim30Gunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.GonyellemeTrendData = tumTarihler.Select(t => gonyGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.OeeTrendData = tumTarihler.Select(t => oeeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var makineList = filtreliVeri
            .GroupBy(x => x.Makine ?? "Bilinmeyen")
            .Select(g => new { Makine = g.Key, Toplam = g.Sum(GetToplamKesimAdet) })
            .OrderByDescending(x => x.Toplam)
            .ToList();

        var ebatlamaMakineAdlari = new[] { "SELCO", "MACMAZZA" };
        foreach (var makineAdi in ebatlamaMakineAdlari)
        {
            if (!makineList.Any(x => string.Equals(x.Makine, makineAdi, StringComparison.OrdinalIgnoreCase)))
            {
                makineList.Add(new { Makine = makineAdi, Toplam = 0d });
            }
        }
        makineList = makineList
            .OrderByDescending(x => x.Toplam)
            .ThenBy(x => x.Makine)
            .ToList();

        viewModel.MakineLabels = makineList.Select(x => x.Makine).ToList();
        viewModel.MakineKesimData = makineList.Select(x => x.Toplam).ToList();

        viewModel.MakineKartlari = periodVeriAllMachinesList
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Makine) ? "Bilinmeyen" : x.Makine.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new MakineKartOzetModel
            {
                MakineAdi = g.Key,
                Uretim = g.Sum(GetToplamKesimAdet),
                DuraklamaDakika = g.Sum(x => x.Duraklama1 + x.Duraklama2),
                Oee = g.Where(x => x.Oee > 0).Select(x => x.Oee).DefaultIfEmpty(0).Average()
            })
            .ToList();

        foreach (var makineAdi in ebatlamaMakineAdlari)
        {
            if (viewModel.MakineKartlari.Any(x => string.Equals(x.MakineAdi, makineAdi, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            viewModel.MakineKartlari.Add(new MakineKartOzetModel
            {
                MakineAdi = makineAdi,
                Uretim = 0,
                DuraklamaDakika = string.Equals(makineAdi, "MACMAZZA", StringComparison.OrdinalIgnoreCase) && macmazzaTekGunEksik
                    ? macmazzaOtoDuraklamaDakika
                    : 0,
                Oee = 0
            });
        }

        viewModel.MakineKartlari = viewModel.MakineKartlari
            .OrderByDescending(x => x.Uretim)
            .ThenBy(x => x.MakineAdi)
            .ToList();

        var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in filtreliVeri)
        {
            var neden1 = string.IsNullOrWhiteSpace(row.DuraklamaNedeni1) ? "Bilinmiyor" : row.DuraklamaNedeni1;
            var neden2 = string.IsNullOrWhiteSpace(row.DuraklamaNedeni2) ? "Bilinmiyor" : row.DuraklamaNedeni2;
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, neden1, row.Duraklama1);
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, neden2, row.Duraklama2);
        }

        if (macmazzaOtoDuraklamaEkle)
        {
            DashboardParsingHelper.AddDuraklama(duraklamaNedenleri, macmazzaOtoDuraklamaNedeni, macmazzaOtoDuraklamaDakika);
        }

        var duraklamaList = duraklamaNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
        viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

        return new DashboardPageResult<EbatlamaDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }

    public async Task<DashboardPageResult<HataliParcaDashboardViewModel>> GetHataliParcaAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;
        var bag = new Dictionary<string, object?>();
        var personelRows = NormalizePersonelRows(snapshot.PersonelRows);

        var viewModel = new HataliParcaDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var excelData = snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .ToList();

        excelData.AddRange(snapshot.ProfilHataRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => new HataliParcaSatirModel
            {
                Tarih = x.Tarih,
                BolumAdi = x.BolumAdi,
                Adet = x.Adet,
                ToplamM2 = 0,
                HataNedeni = x.HataNedeni
            }));

        var boyaHataUretimRows = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue && x.HataliParcaSayisi > 0)
            .Select(x => new HataliParcaSatirModel
            {
                Tarih = x.Tarih,
                BolumAdi = "Boyahane",
                Adet = x.HataliParcaSayisi,
                ToplamM2 = 0,
                HataNedeni = string.IsNullOrWhiteSpace(x.Aciklama) ? "Boyahane Hatalı Parça" : x.Aciklama
            })
            .ToList();

        if (boyaHataUretimRows.Any())
        {
            excelData.AddRange(boyaHataUretimRows);
        }
        else
        {
            excelData.AddRange(snapshot.BoyaHataRows
                .Where(x => x.Tarih != DateTime.MinValue)
                .Select(x => new HataliParcaSatirModel
                {
                    Tarih = x.Tarih,
                    BolumAdi = "Boyahane",
                    Adet = x.HataliAdet,
                    ToplamM2 = 0,
                    HataNedeni = x.HataNedeni
                }));
        }

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<HataliParcaDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        bool IsMakineHatasi(string? neden)
        {
            var normalized = DashboardParsingHelper.NormalizeLabel(neden);
            return normalized.Contains("makine hatası", StringComparison.OrdinalIgnoreCase);
        }

        var analizVerisi = excelData.Where(x => !IsMakineHatasi(x.HataNedeni)).ToList();
        var tarihKaynak = analizVerisi.Any() ? analizVerisi : excelData;

        AddFilterAvailabilityMetadata(bag, tarihKaynak.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(tarihKaynak.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliVeri = analizVerisi.AsQueryable();
        int? yearToUse = null;
        if (hasDateRange)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            bag["HataliTrendTitle"] = "Hatalı Parça Trendi (Tarih Aralığı)";
            bag["HataliM2Title"] = "Hatalı m² Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue)
        {
            var resolvedYear = DashboardParsingHelper.ResolveYearForMonth(tarihKaynak.Select(x => x.Tarih), ay.Value, yil);
            yearToUse = resolvedYear ?? yil ?? islenecekTarih.Year;
            if (resolvedYear.HasValue && (!yil.HasValue || yil.Value != resolvedYear.Value))
            {
                bag["HataliResolvedYear"] = resolvedYear.Value;
            }

            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yearToUse.Value);
            bag["HataliTrendTitle"] = "Hatalı Parça Trendi (Aylık)";
            bag["HataliM2Title"] = "Hatalı m² Trendi (Aylık)";
        }
        else
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["HataliTrendTitle"] = "Hatalı Parça Trendi (Son 7 Gün)";
            bag["HataliM2Title"] = "Hatalı m² Trendi (Son 7 Gün)";
        }

        viewModel.CalisilanIsGunu = CountDistinctWorkingDays(filtreliVeri.Select(x => x.Tarih));
        var hataliPersonelStart = hasDateRange
            ? rangeStart!.Value
            : ay.HasValue
                ? new DateTime(yearToUse ?? yil ?? islenecekTarih.Year, ay.Value, 1)
                : islenecekTarih.Date;
        var hataliPersonelEnd = hasDateRange
            ? rangeEnd!.Value
            : ay.HasValue
                ? new DateTime(yearToUse ?? yil ?? islenecekTarih.Year, ay.Value, 1).AddMonths(1).AddDays(-1)
                : islenecekTarih.Date;
        viewModel.OrtalamaCalisanPersonel = CalculateRoundedAveragePersonnel(personelRows, hataliPersonelStart, hataliPersonelEnd);
        viewModel.ToplamHataAdet = filtreliVeri.Sum(x => x.Adet);
        viewModel.ToplamHataM2 = filtreliVeri.Sum(x => x.ToplamM2);

        var topNeden = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.HataNedeni))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (topNeden != null)
        {
            viewModel.EnCokHataNedeni = $"{topNeden.Key} ({topNeden.Total:N0})";
        }

        var topBolum = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.BolumAdi))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (topBolum != null)
        {
            viewModel.EnCokHataBolum = $"{topBolum.Key} ({topBolum.Total:N0})";
        }

        var topOperator = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.OperatorAdi))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (topOperator != null)
        {
            viewModel.EnCokOperator = $"{topOperator.Key} ({topOperator.Total:N0})";
        }

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
        }
        else if (ay.HasValue)
        {
            var yearForTrend = yearToUse ?? yil ?? islenecekTarih.Year;
            trendBaslangic = new DateTime(yearForTrend, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var hataAdetGunluk = analizVerisi.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Adet));
        var hataM2Gunluk = analizVerisi.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ToplamM2));

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.HataAdetTrendData = tumTarihler.Select(t => hataAdetGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.HataM2TrendData = tumTarihler.Select(t => hataM2Gunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var hataNedenList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.HataNedeni))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .ToList();
        viewModel.HataNedenLabels = hataNedenList.Select(x => x.Key).ToList();
        viewModel.HataNedenData = hataNedenList.Select(x => x.Total).ToList();

        var bolumList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.BolumAdi))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .ToList();
        viewModel.BolumLabels = bolumList.Select(x => x.Key).ToList();
        viewModel.BolumData = bolumList.Select(x => x.Total).ToList();

        viewModel.BolumBazliHataNedenleri = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.BolumAdi))
            .AsEnumerable()
            .Select(g =>
            {
                var nedenler = g
                    .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.HataNedeni))
                    .Select(ng => new { Neden = ng.Key, Toplam = ng.Sum(x => x.Adet) })
                    .OrderByDescending(x => x.Toplam)
                    .ToList();

                return new BolumBazliHataNedenViewModel
                {
                    Bolum = g.Key,
                    NedenLabels = nedenler.Select(x => x.Neden).ToList(),
                    NedenData = nedenler.Select(x => x.Toplam).ToList()
                };
            })
            .OrderBy(x => x.Bolum)
            .ToList();

        var operatorList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.OperatorAdi))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();
        viewModel.OperatorLabels = operatorList.Select(x => x.Key).ToList();
        viewModel.OperatorData = operatorList.Select(x => x.Total).ToList();

        var kalinlikList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Kalinlik))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .ToList();
        viewModel.KalinlikLabels = kalinlikList.Select(x => x.Key).ToList();
        viewModel.KalinlikData = kalinlikList.Select(x => x.Total).ToList();

        var renkList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.Renk))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();
        viewModel.RenkLabels = renkList.Select(x => x.Key).ToList();
        viewModel.RenkData = renkList.Select(x => x.Total).ToList();

        var kesimList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.KesimDurumu))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .ToList();
        viewModel.KesimDurumLabels = kesimList.Select(x => x.Key).ToList();
        viewModel.KesimDurumData = kesimList.Select(x => x.Total).ToList();

        var pvcList = filtreliVeri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.PvcDurumu))
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Total)
            .ToList();
        viewModel.PvcDurumLabels = pvcList.Select(x => x.Key).ToList();
        viewModel.PvcDurumData = pvcList.Select(x => x.Total).ToList();

        return new DashboardPageResult<HataliParcaDashboardViewModel>
        {
            Model = viewModel,
            ViewBagValues = bag
        };
    }
}
