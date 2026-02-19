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

    public async Task<DashboardPageResult<GenelFabrikaOzetViewModel>> GetGunlukVerilerAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var model = new GenelFabrikaOzetViewModel();
        var bag = new Dictionary<string, object?>();

        var profilRows = snapshot.ProfilRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, Uretim: (double)x.UretimAdedi))
            .ToList();

        var boyaRows = snapshot.BoyaUretimRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, Uretim: (double)(x.PanelAdet + x.DosemeAdet)))
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
                Uretim: x.ToplamKesimAdet,
                Duraklama: x.Duraklama1 + x.Duraklama2,
                Performans: x.Performans,
                Kullanilabilirlik: x.Kullanilabilirlik,
                Kalite: x.Kalite,
                Oee: x.Oee))
            .ToList();

        var hataliRows = new List<(DateTime Tarih, double Adet, double M2, string? Neden, string? Bolum, string? Operator)>();

        hataliRows.AddRange(snapshot.HataliParcaRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, x.Adet, x.ToplamM2, x.HataNedeni, x.BolumAdi, x.OperatorAdi)));

        hataliRows.AddRange(snapshot.ProfilHataRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, (double)x.Adet, 0d, x.HataNedeni, x.BolumAdi, (string?)null)));

        hataliRows.AddRange(snapshot.BoyaHataRows
            .Where(x => x.Tarih != DateTime.MinValue)
            .Select(x => (x.Tarih, (double)x.HataliAdet, 0d, x.HataNedeni, (string?)"Boyahane", (string?)null)));

        var allDates = profilRows.Select(x => x.Tarih.Date)
            .Concat(boyaRows.Select(x => x.Tarih.Date))
            .Concat(pvcRows.Select(x => x.Tarih.Date))
            .Concat(masterRows.Select(x => x.Tarih.Date))
            .Concat(skipperRows.Select(x => x.Tarih.Date))
            .Concat(roverBRows.Select(x => x.Tarih.Date))
            .Concat(tezgahRows.Select(x => x.Tarih.Date))
            .Concat(ebatlamaRows.Select(x => x.Tarih.Date))
            .Concat(hataliRows.Select(x => x.Tarih.Date))
            .ToList();

        var maxDate = allDates.Any() ? allDates.Max() : DateTime.Today;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;
        var isSingleDayRange = hasDateRange && rangeStart!.Value.Date == rangeEnd!.Value.Date;

        DateTime ozetStart;
        DateTime ozetEnd;
        DateTime trendStart;
        DateTime trendEnd;
        if (hasDateRange)
        {
            ozetStart = rangeStart!.Value;
            ozetEnd = rangeEnd!.Value;
            trendStart = ozetStart;
            trendEnd = ozetEnd;
            bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy} - {ozetEnd:dd.MM.yyyy}";
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
            ozetEnd = ozetStart.AddMonths(1).AddDays(-1);
            trendStart = ozetStart;
            trendEnd = ozetEnd;
            bag["OzetRange"] = $"{ozetStart:dd.MM.yyyy} - {ozetEnd:dd.MM.yyyy}";
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

        var trendTarihleri = Enumerable.Range(0, (trendEnd - trendStart).Days + 1)
            .Select(offset => trendStart.AddDays(offset))
            .ToList();

        var uretimGunluk = ozetTarihleri.ToDictionary(t => t, _ => 0d);
        var hataGunluk = ozetTarihleri.ToDictionary(t => t, _ => 0d);
        var duraklamaGunluk = ozetTarihleri.ToDictionary(t => t, _ => 0d);
        var uretimTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var hataTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var duraklamaTrendGunluk = trendTarihleri.ToDictionary(t => t, _ => 0d);
        var bolumKatki = new Dictionary<string, double>();

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

        foreach (var row in profilRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDept(bolumKatki, "Profil Lazer", row.Uretim);
            }
        }

        foreach (var row in boyaRows)
        {
            AddDaily(uretimTrendGunluk, row.Tarih, row.Uretim);
            if (row.Tarih.Date >= ozetStart && row.Tarih.Date <= ozetEnd)
            {
                AddDaily(uretimGunluk, row.Tarih, row.Uretim);
                AddDept(bolumKatki, "Boyahane", row.Uretim);
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

        model.ToplamUretim = uretimGunluk.Values.Sum();
        model.ToplamHataAdet = filteredHatali.Sum(x => x.Adet);
        model.ToplamHataM2 = filteredHatali.Sum(x => x.M2);
        model.ToplamDuraklamaDakika = duraklamaGunluk.Values.Sum();

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
        model.OrtalamaOee = oeeValues.Any() ? oeeValues.Average() : 0;

        var machineOeeRows = new List<(string Machine, double Oee)>();
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
        model.UretimTrendData = trendTarihleri.Select(t => uretimTrendGunluk[t]).ToList();
        model.HataTrendData = trendTarihleri.Select(t => hataTrendGunluk[t]).ToList();
        model.DuraklamaTrendData = trendTarihleri.Select(t => duraklamaTrendGunluk[t]).ToList();

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

    public async Task<DashboardPageResult<GunlukVerilerViewModel>> GetProfilLazerAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default)
    {
        var snapshot = await _ingestionService.GetSnapshotAsync(cancellationToken);
        var secilenTarih = raporTarihi?.Date;

        var viewModel = new GunlukVerilerViewModel
        {
            RaporTarihi = secilenTarih ?? DateTime.Today,
            ProfilIsimleri = new List<string>(),
            ProfilUretimAdetleri = new List<int>(),
            Son7GunTarihleri = new List<string>(),
            GunlukUretimSayilari = new List<int>(),
            UrunIsimleri = new List<string>(),
            UrunHarcananSure = new List<int>(),
            HataNedenleri = new List<string>(),
            HataNedenAdetleri = new List<int>(),
            HataUrunSonuclari = new List<string>(),
            HataUrunSonucAdetleri = new List<int>()
        };

        var bag = new Dictionary<string, object?>();

        var excelData = snapshot.ProfilRows.Where(x => x.Tarih != DateTime.MinValue).ToList();
        var hataExcelData = snapshot.ProfilHataRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!excelData.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<GunlukVerilerViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var gununVerileri = excelData.AsQueryable();
        if (hasDateRange)
        {
            gununVerileri = gununVerileri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
        }
        else if (ay.HasValue && yil.HasValue)
        {
            gununVerileri = gununVerileri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
        }
        else
        {
            gununVerileri = gununVerileri.Where(x => x.Tarih.Date == islenecekTarih.Date);
        }

        viewModel.GunlukToplamUretim = gununVerileri.Sum(x => x.UretimAdedi);
        viewModel.GunlukToplamSure = gununVerileri.Sum(x => x.CalismaSuresi);
        viewModel.OrtalamaIslemSuresi = viewModel.GunlukToplamUretim > 0
            ? (double)viewModel.GunlukToplamSure / viewModel.GunlukToplamUretim
            : 0;

        var profilHataVerileri = hataExcelData
            .Where(x =>
            {
                var bolum = (x.BolumAdi ?? string.Empty).ToLowerInvariant();
                return bolum.Contains("metal") || bolum.Contains("profil") || bolum.Contains("lazer");
            })
            .AsQueryable();

        if (hasDateRange)
        {
            profilHataVerileri = profilHataVerileri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
        }
        else if (ay.HasValue && yil.HasValue)
        {
            profilHataVerileri = profilHataVerileri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
        }
        else
        {
            profilHataVerileri = profilHataVerileri.Where(x => x.Tarih.Date == islenecekTarih.Date);
        }

        viewModel.HataliUrunAdedi = profilHataVerileri.Sum(x => x.Adet);
        viewModel.HurdaAdedi = profilHataVerileri
            .Where(x => (x.HataUrunSonucu ?? string.Empty).ToLowerInvariant().Contains("hurda"))
            .Sum(x => x.Adet);

        var hataNedenGruplari = profilHataVerileri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.HataNedeni))
            .Select(g => new { Neden = g.Key, Toplam = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Toplam)
            .ToList();

        viewModel.HataNedenleri = hataNedenGruplari.Select(x => x.Neden).ToList();
        viewModel.HataNedenAdetleri = hataNedenGruplari.Select(x => x.Toplam).ToList();

        var hataUrunGruplari = profilHataVerileri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.HataUrunSonucu))
            .Select(g => new { Sonuc = g.Key, Toplam = g.Sum(x => x.Adet) })
            .OrderByDescending(x => x.Toplam)
            .ToList();

        viewModel.HataUrunSonuclari = hataUrunGruplari.Select(x => x.Sonuc).ToList();
        viewModel.HataUrunSonucAdetleri = hataUrunGruplari.Select(x => x.Toplam).ToList();

        var pastaGrafikData = gununVerileri
            .AsEnumerable()
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.ProfilTipi))
            .Select(g => new { Profil = g.Key, ToplamUretim = g.Sum(x => x.UretimAdedi) })
            .OrderByDescending(x => x.ToplamUretim)
            .ToList();

        viewModel.ProfilIsimleri = pastaGrafikData.Select(x => x.Profil).Where(p => !string.IsNullOrWhiteSpace(p)).Cast<string>().ToList();
        viewModel.ProfilUretimAdetleri = pastaGrafikData.Select(x => x.ToplamUretim).ToList();

        var urunBazliSureData = gununVerileri
            .AsEnumerable()
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.ProfilTipi))
            .Select(g => new { Urun = g.Key, ToplamSure = g.Sum(x => x.CalismaSuresi) })
            .OrderByDescending(x => x.ToplamSure)
            .ToList();

        var toplamSureTumUrunler = (double)urunBazliSureData.Sum(x => x.ToplamSure);
        viewModel.UrunIsimleri = urunBazliSureData.Select(x => x.Urun).Where(u => !string.IsNullOrWhiteSpace(u)).Cast<string>().ToList();
        viewModel.UrunHarcananSure = urunBazliSureData
            .Select(x => toplamSureTumUrunler > 0 ? (int)Math.Round(x.ToplamSure / toplamSureTumUrunler * 100) : 0)
            .ToList();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
            bag["TrendTitle"] = "Seçili Tarih Aralığı Üretim Trendi";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
            bag["TrendTitle"] = "Aylık Üretim Trendi";
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
            bag["TrendTitle"] = "Son 7 Günlük Üretim Trendi";
        }

        var trendVerileri = excelData
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .Select(g => new { Tarih = g.Key, ToplamUretim = g.Sum(x => x.UretimAdedi) })
            .OrderBy(x => x.Tarih)
            .ToDictionary(x => x.Tarih, x => x.ToplamUretim);

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        viewModel.Son7GunTarihleri = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.GunlukUretimSayilari = tumTarihler.Select(t => trendVerileri.TryGetValue(t, out var toplam) ? toplam : 0).ToList();

        var hataTrendVerileri = hataExcelData
            .Where(x =>
            {
                var bolum = (x.BolumAdi ?? string.Empty).ToLowerInvariant();
                return bolum.Contains("metal") || bolum.Contains("profil") || bolum.Contains("lazer");
            })
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Adet));

        viewModel.GunlukHataliUrunSayilari = tumTarihler.Select(t => hataTrendVerileri.TryGetValue(t, out var toplam) ? toplam : 0).ToList();

        return new DashboardPageResult<GunlukVerilerViewModel>
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

        var viewModel = new BoyaDashboardViewModel { RaporTarihi = secilenTarih ?? DateTime.Today };
        var uretimListesi = snapshot.BoyaUretimRows.Where(x => x.Tarih != DateTime.MinValue).ToList();
        var hataListesi = snapshot.BoyaHataRows.Where(x => x.Tarih != DateTime.MinValue).ToList();

        if (!uretimListesi.Any() && !hataListesi.Any())
        {
            bag["ErrorMessage"] = "Dashboard verisi henüz hazır değil. Lütfen daha sonra tekrar deneyin.";
            return new DashboardPageResult<BoyaDashboardViewModel> { Model = viewModel, ViewBagValues = bag };
        }

        var tarihKaynaklari = uretimListesi.Select(x => x.Tarih).Concat(hataListesi.Select(x => x.Tarih));
        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(tarihKaynaklari, DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var gununUretimVerileri = uretimListesi.AsQueryable();
        var gununHataVerileri = hataListesi.AsQueryable();

        if (hasDateRange)
        {
            gununUretimVerileri = gununUretimVerileri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            gununHataVerileri = gununHataVerileri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
        }
        else if (ay.HasValue && yil.HasValue)
        {
            gununUretimVerileri = gununUretimVerileri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
            gununHataVerileri = gununHataVerileri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
        }
        else
        {
            gununUretimVerileri = gununUretimVerileri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            gununHataVerileri = gununHataVerileri.Where(x => x.Tarih.Date == islenecekTarih.Date);
        }

        viewModel.PanelToplamBoyama = gununUretimVerileri.Sum(x => x.PanelAdet);
        viewModel.DosemeToplamBoyama = gununUretimVerileri.Sum(x => x.DosemeAdet);
        viewModel.GunlukToplamBoyama = viewModel.PanelToplamBoyama + viewModel.DosemeToplamBoyama;
        viewModel.GunlukHataSayisi = gununHataVerileri.Sum(x => x.HataliAdet);
        viewModel.FireOrani = viewModel.GunlukToplamBoyama > 0
            ? (viewModel.GunlukHataSayisi / viewModel.GunlukToplamBoyama) * 100
            : 0;

        var hataGruplari = gununHataVerileri
            .GroupBy(x => DashboardParsingHelper.NormalizeLabel(x.HataNedeni))
            .Select(g => new { Neden = g.Key, Toplam = g.Sum(x => x.HataliAdet) })
            .OrderByDescending(x => x.Toplam)
            .ToList();

        viewModel.HataNedenleriListesi = hataGruplari.Select(x => x.Neden).ToList();
        viewModel.HataSayilariListesi = hataGruplari.Select(x => (int)x.Toplam).ToList();

        DateTime trendBaslangic;
        DateTime trendBitis;
        if (hasDateRange)
        {
            trendBaslangic = rangeStart!.Value;
            trendBitis = rangeEnd!.Value;
            bag["UretimDagilimiTitle"] = "Tarih Aralığı Üretim Dağılımı (Panel vs Döşeme)";
            bag["KaliteTrendTitle"] = "Kalite Trendi (Tarih Aralığı)";
            bag["UretimTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
        }
        else if (ay.HasValue && yil.HasValue)
        {
            trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
            trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
            bag["UretimDagilimiTitle"] = "Aylık Üretim Dağılımı (Panel vs Döşeme)";
            bag["KaliteTrendTitle"] = "Kalite Trendi (Aylık)";
            bag["UretimTrendTitle"] = "Üretim Trendi (Aylık)";
        }
        else
        {
            var referansTarih = islenecekTarih;
            trendBaslangic = referansTarih.AddDays(-6);
            trendBitis = referansTarih;
            bag["UretimDagilimiTitle"] = "Üretim Dağılımı (Panel vs Döşeme)";
            bag["KaliteTrendTitle"] = "Kalite Trendi (Son 7 Gün)";
            bag["UretimTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
        }

        var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
            .Select(offset => trendBaslangic.Date.AddDays(offset))
            .ToList();

        var uretimDagilimi = uretimListesi
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .Select(g => new { Tarih = g.Key, Panel = g.Sum(x => x.PanelAdet), Doseme = g.Sum(x => x.DosemeAdet) })
            .OrderBy(x => x.Tarih)
            .ToDictionary(x => x.Tarih, x => x);

        viewModel.UretimDagilimi.Labels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.UretimDagilimi.PanelData = tumTarihler.Select(t => uretimDagilimi.TryGetValue(t, out var v) ? v.Panel : 0).ToList();
        viewModel.UretimDagilimi.DosemeData = tumTarihler.Select(t => uretimDagilimi.TryGetValue(t, out var v) ? v.Doseme : 0).ToList();

        var hataDagilimi = hataListesi
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .Select(g => new { Tarih = g.Key, ToplamHata = g.Sum(x => x.HataliAdet) })
            .OrderBy(x => x.Tarih)
            .ToDictionary(x => x.Tarih, x => x.ToplamHata);

        viewModel.KaliteTrendi.Labels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.KaliteTrendi.Data = tumTarihler.Select(t => hataDagilimi.TryGetValue(t, out var toplam) ? toplam : 0).ToList();

        var uretimTrend = uretimListesi
            .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .Select(g => new { Tarih = g.Key, ToplamUretim = g.Sum(x => x.PanelAdet + x.DosemeAdet) })
            .OrderBy(x => x.Tarih)
            .ToDictionary(x => x.Tarih, x => x.ToplamUretim);

        viewModel.UretimTrendi.Labels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.UretimTrendi.Data = tumTarihler.Select(t => uretimTrend.TryGetValue(t, out var toplam) ? toplam : 0).ToList();

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

        viewModel.ToplamDelik = filtreliVeri.Sum(x => x.DelikSayisi);
        viewModel.ToplamDelikFreeze = filtreliVeri.Sum(x => x.DelikFreezeSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
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

        viewModel.ToplamDelik = filtreliVeri.Sum(x => x.DelikSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
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

        viewModel.ToplamDelikFreeze = filtreliVeri.Sum(x => x.DelikFreezeSayisi);
        viewModel.ToplamDelikFreezePvc = filtreliVeri.Sum(x => x.DelikFreezePvcSayisi);
        viewModel.ToplamHataliParca = filtreliVeri.Sum(x => x.HataliParca);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
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

        var islenecekTarih = secilenTarih ?? ResolveClosestAvailableDate(excelData.Select(x => x.Tarih), DateTime.Today);
        viewModel.RaporTarihi = islenecekTarih;
        var (rangeStart, rangeEnd) = NormalizeDateRange(baslangicTarihi, bitisTarihi);
        var hasDateRange = rangeStart.HasValue && rangeEnd.HasValue;

        var filtreliVeri = excelData.AsQueryable();
        int? yearToUse = null;
        if (hasDateRange)
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date >= rangeStart!.Value && x.Tarih.Date <= rangeEnd!.Value);
            bag["TezgahTrendTitle"] = "Üretim Trendi (Tarih Aralığı)";
            bag["TezgahKisiTrendTitle"] = "Kişi Sayısı (Tarih Aralığı)";
            bag["TezgahKullanilabilirlikTitle"] = "Kullanılabilirlik (Tarih Aralığı)";
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
            bag["TezgahTrendTitle"] = "Üretim Trendi (Aylık)";
            bag["TezgahKisiTrendTitle"] = "Kişi Sayısı (Aylık)";
            bag["TezgahKullanilabilirlikTitle"] = "Kullanılabilirlik (Aylık)";
        }
        else
        {
            filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            bag["TezgahTrendTitle"] = "Üretim Trendi (Son 7 Gün)";
            bag["TezgahKisiTrendTitle"] = "Kişi Sayısı (Son 7 Gün)";
            bag["TezgahKullanilabilirlikTitle"] = "Kullanılabilirlik (Son 7 Gün)";
        }

        viewModel.ToplamParcaAdeti = filtreliVeri.Sum(x => x.ParcaAdeti);
        viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
        viewModel.ToplamKayipSureDakika = filtreliVeri.Sum(x => x.KayipSureDakika);
        viewModel.OrtalamaKullanilabilirlik = filtreliVeri.Any() ? filtreliVeri.Average(x => x.Kullanilabilirlik) : 0;

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

        var parcaGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ParcaAdeti));
        var kisiGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.KisiSayisi));
        var kullanilabilirlikGunluk = excelData.Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
            .GroupBy(x => x.Tarih.Date)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Kullanilabilirlik));

        viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
        viewModel.ParcaTrendData = tumTarihler.Select(t => parcaGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KisiTrendData = tumTarihler.Select(t => kisiGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
        viewModel.KullanilabilirlikTrendData = tumTarihler.Select(t => kullanilabilirlikGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

        var kayipNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in filtreliVeri)
        {
            DashboardParsingHelper.AddDuraklama(kayipNedenleri, row.KayipSureNedeni, row.KayipSureDakika);
        }

        var kayipList = kayipNedenleri.OrderByDescending(x => x.Value).ToList();
        viewModel.KayipNedenLabels = kayipList.Select(x => x.Key).ToList();
        viewModel.KayipNedenData = kayipList.Select(x => x.Value).ToList();

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

        var seciliMakine = NormalizeMachine(makine);
        var seciliMakineSatirlari = string.IsNullOrWhiteSpace(seciliMakine)
            ? new List<EbatlamaSatirModel>()
            : excelData.Where(x => IsSameMachine(x.Makine, seciliMakine!)).ToList();
        if (!string.IsNullOrWhiteSpace(seciliMakine) && !seciliMakineSatirlari.Any())
        {
            seciliMakine = null;
        }

        var tarihKaynak = string.IsNullOrWhiteSpace(seciliMakine) ? excelData : seciliMakineSatirlari;
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

        var seciliMakineMacmazza = IsMacmazzaMachine(seciliMakine);
        var macmazzaKaydiVarTumMakineler = periodVeriAllMachinesList.Any(x => IsMacmazzaMachine(x.Makine));
        var tekGunModu = !ay.HasValue && (!hasDateRange || isSingleDayRange);
        var macmazzaTekGunEksik = tekGunModu && !macmazzaKaydiVarTumMakineler;
        var macmazzaOtoDuraklamaEkle = macmazzaTekGunEksik && (string.IsNullOrWhiteSpace(seciliMakine) || seciliMakineMacmazza);
        var macmazzaEkDuraklama = macmazzaOtoDuraklamaEkle ? macmazzaOtoDuraklamaDakika : 0d;

        viewModel.ToplamKesimAdet = filtreliVeri.Sum(x => x.ToplamKesimAdet);
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
            .GroupBy(x => x.Tarih.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.ToplamKesimAdet));
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
            .Select(g => new { Makine = g.Key, Toplam = g.Sum(x => x.ToplamKesimAdet) })
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
                Uretim = g.Sum(x => x.ToplamKesimAdet),
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
