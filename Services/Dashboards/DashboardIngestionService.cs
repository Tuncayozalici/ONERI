using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using ONERI.Data;
using ONERI.Models;
using System.Data;
using System.Text.Json;

namespace ONERI.Services.Dashboards;

public class DashboardIngestionService : IDashboardIngestionService
{
    private const string CacheKey = "dashboard_data_snapshot_v1";
    private const string DbRecordKey = "dashboard_data_snapshot_v1";

    private readonly IMemoryCache _cache;
    private readonly FabrikaContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly ILogger<DashboardIngestionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public DashboardIngestionService(
        IMemoryCache cache,
        FabrikaContext context,
        IWebHostEnvironment hostingEnvironment,
        ILogger<DashboardIngestionService> logger)
    {
        _cache = cache;
        _context = context;
        _hostingEnvironment = hostingEnvironment;
        _logger = logger;
    }

    public async Task<DashboardDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out DashboardDataSnapshot? snapshot) && snapshot != null)
        {
            return snapshot;
        }

        await EnsureStorageTableAsync(cancellationToken);

        var storedSnapshot = await TryReadStoredSnapshotAsync(cancellationToken);
        if (storedSnapshot != null)
        {
            SetCache(storedSnapshot);
            return storedSnapshot;
        }

        return new DashboardDataSnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var freshSnapshot = ParseSnapshotFromExcel();
        await EnsureStorageTableAsync(cancellationToken);
        await SaveSnapshotAsync(freshSnapshot, cancellationToken);
        SetCache(freshSnapshot);
    }

    private void SetCache(DashboardDataSnapshot snapshot)
    {
        _cache.Set(CacheKey, snapshot, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(20),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });
    }

    private async Task EnsureStorageTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS DashboardCacheEntries (
                CacheKey TEXT PRIMARY KEY,
                PayloadJson TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
            """;

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<DashboardDataSnapshot?> TryReadStoredSnapshotAsync(CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT PayloadJson FROM DashboardCacheEntries WHERE CacheKey = $key LIMIT 1;";

        var keyParam = command.CreateParameter();
        keyParam.ParameterName = "$key";
        keyParam.Value = DbRecordKey;
        command.Parameters.Add(keyParam);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is not string json || string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DashboardDataSnapshot>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard cache kaydı okunamadı, Excel'den yeniden ingest edilecek.");
            return null;
        }
    }

    private async Task SaveSnapshotAsync(DashboardDataSnapshot snapshot, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(snapshot, _jsonOptions);

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DashboardCacheEntries (CacheKey, PayloadJson, UpdatedAtUtc)
            VALUES ($key, $payload, $updated)
            ON CONFLICT(CacheKey) DO UPDATE SET
                PayloadJson = excluded.PayloadJson,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        var keyParam = command.CreateParameter();
        keyParam.ParameterName = "$key";
        keyParam.Value = DbRecordKey;
        command.Parameters.Add(keyParam);

        var payloadParam = command.CreateParameter();
        payloadParam.ParameterName = "$payload";
        payloadParam.Value = json;
        command.Parameters.Add(payloadParam);

        var updatedParam = command.CreateParameter();
        updatedParam.ParameterName = "$updated";
        updatedParam.Value = DateTime.UtcNow.ToString("O");
        command.Parameters.Add(updatedParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DashboardDataSnapshot ParseSnapshotFromExcel()
    {
        var excelRoot = Path.Combine(_hostingEnvironment.WebRootPath, "EXCELS");

        var snapshot = new DashboardDataSnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ProfilRows = ParseProfilRows(excelRoot),
            ProfilHataRows = ParseProfilHataRows(excelRoot),
            BoyaUretimRows = ParseBoyaUretimRows(excelRoot),
            BoyaHataRows = ParseBoyaHataRows(excelRoot),
            PvcRows = ParsePvcRows(excelRoot),
            MasterwoodRows = ParseMasterwoodRows(excelRoot),
            SkipperRows = ParseSkipperRows(excelRoot),
            RoverBRows = ParseRoverBRows(excelRoot),
            TezgahRows = ParseTezgahRows(excelRoot),
            EbatlamaRows = ParseEbatlamaRows(excelRoot),
            PersonelRows = ParsePersonelRows(excelRoot),
            HataliParcaRows = ParseHataliParcaRows(excelRoot)
        };

        return snapshot;
    }

    private List<SatirModeli> ParseProfilRows(string excelRoot)
    {
        var result = new List<SatirModeli>();
        var filePath = Path.Combine(excelRoot, "MARWOOD Profil Lazer Veri Ekranı.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets["LAZER KAYIT"];
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                result.Add(new SatirModeli
                {
                    Tarih = parsedDate,
                    MusteriAdi = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                    ProfilTipi = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                    UretimAdedi = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 5].Value),
                    CalismaSuresi = DashboardParsingHelper.ParseCalismaSuresiDakika(worksheet.Cells[row, 6].Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Profil Lazer satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<ProfilHataSatir> ParseProfilHataRows(string excelRoot)
    {
        var result = new List<ProfilHataSatir>();
        var filePath = Path.Combine(excelRoot, "METAL HATALI  PARÇA GİRİŞİ.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault(x =>
            x.Name.Equals("VERİ KAYIT", StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals("VERI KAYIT", StringComparison.OrdinalIgnoreCase));
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colTarih = DashboardParsingHelper.FindColumn(worksheet, "TARİH", "TARIH");
        int colBolum = DashboardParsingHelper.FindColumn(worksheet, "BÖLÜM ADI", "BOLUM ADI");
        int colAdet = DashboardParsingHelper.FindColumn(worksheet, "ADET");
        int colSonuc = DashboardParsingHelper.FindColumn(worksheet, "HATALI ÜRÜN NE OLACAK?", "HATALI URUN NE OLACAK");
        int colNeden = DashboardParsingHelper.FindColumn(worksheet, "HATA NEDENİ", "HATA NEDENI");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                int tarihCol = colTarih > 0 ? colTarih : 1;
                int bolumCol = colBolum > 0 ? colBolum : 2;
                int adetCol = colAdet > 0 ? colAdet : 5;
                int sonucCol = colSonuc > 0 ? colSonuc : 6;
                int nedenCol = colNeden > 0 ? colNeden : 7;
                var dateCell = worksheet.Cells[row, tarihCol];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                result.Add(new ProfilHataSatir
                {
                    Tarih = parsedDate,
                    BolumAdi = worksheet.Cells[row, bolumCol].Value?.ToString()?.Trim(),
                    HataUrunSonucu = worksheet.Cells[row, sonucCol].Value?.ToString()?.Trim(),
                    HataNedeni = worksheet.Cells[row, nedenCol].Value?.ToString()?.Trim(),
                    Adet = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, adetCol].Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Profil Hata satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<BoyaUretimSatir> ParseBoyaUretimRows(string excelRoot)
    {
        var result = new List<BoyaUretimSatir>();
        var filePath = Path.Combine(excelRoot, "YENİ BOYA GÜNLÜK VERİ TAKİP 2026 YILI.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets["VERİ KAYIT"];
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                result.Add(new BoyaUretimSatir
                {
                    Tarih = parsedDate,
                    PanelAdet = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 4].Value),
                    DosemeAdet = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 6].Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Boyahane üretim satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<BoyaHataSatir> ParseBoyaHataRows(string excelRoot)
    {
        var result = new List<BoyaHataSatir>();
        var filePath = Path.Combine(excelRoot, "BOYA HATALI  PARÇA GİRİŞİ.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets["VERİ KAYIT"];
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                result.Add(new BoyaHataSatir
                {
                    Tarih = parsedDate,
                    HataNedeni = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                    HataliAdet = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 5].Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Boyahane hata satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<PvcSatirModel> ParsePvcRows(string excelRoot)
    {
        var result = new List<PvcSatirModel>();
        var filePath = Path.Combine(excelRoot, "PVC BÖLÜMÜ VERİ EKRANI 2026.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets["KAYIT"];
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colCount = worksheet.Dimension.Columns;
        int hataliParcaCol = DashboardParsingHelper.FindColumn(worksheet, "HATALI PARÇA", "HATALI PARCA");
        int aciklamaCol = DashboardParsingHelper.FindColumn(worksheet, "AÇIKLAMA", "ACIKLAMA");
        int performansCol = DashboardParsingHelper.FindColumn(worksheet, "PERFORMANS");
        int uretimOraniCol = DashboardParsingHelper.FindColumn(worksheet, "ÜRETİM ORANI", "URETIM ORANI", "ÜRETİMORANI", "URETIMORANI");
        int kayipSureCol = DashboardParsingHelper.FindColumn(worksheet, "KAYIP SÜRE", "KAYIP SURE", "KAYIP SÜRE ORANI", "KAYIP SURE ORANI");
        int kullanilabilirlikCol = DashboardParsingHelper.FindColumn(worksheet, "KULLANILABİLİRLİK", "KULLANILABILIRLIK");
        int kaliteCol = DashboardParsingHelper.FindColumn(worksheet, "KALİTE", "KALITE");
        int oeeCol = DashboardParsingHelper.FindColumn(worksheet, "OEE");
        int fiiliCalismaCol = DashboardParsingHelper.FindColumn(worksheet, "FİİLİ ÇALIŞMA ORANI", "FIILI CALISMA ORANI", "FIILI CALISMAORANI", "FİİLİ ÇALIŞMA");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                var hataliParca = hataliParcaCol > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, hataliParcaCol].Value)
                    : (colCount >= 12 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 12].Value) : 0);
                var aciklama = aciklamaCol > 0
                    ? worksheet.Cells[row, aciklamaCol].Value?.ToString()?.Trim()
                    : (colCount >= 13 ? worksheet.Cells[row, 13].Value?.ToString()?.Trim() : (colCount >= 12 ? worksheet.Cells[row, 12].Value?.ToString()?.Trim() : null));
                var performans = performansCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, performansCol].Value)
                    : (colCount >= 14 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 14].Value) : (uretimOraniCol > 0 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, uretimOraniCol].Value) : 0));
                var kayipSure = kayipSureCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kayipSureCol].Value)
                    : (colCount >= 15 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 15].Value) : (colCount >= 14 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 14].Value) : 0));
                var kullanilabilirlik = kullanilabilirlikCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kullanilabilirlikCol].Value)
                    : (colCount >= 16 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 16].Value) : 0);
                var kalite = kaliteCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kaliteCol].Value)
                    : (colCount >= 17 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 17].Value) : 0);
                var oee = oeeCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, oeeCol].Value)
                    : (colCount >= 18 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 18].Value) : 0);
                var fiiliCalisma = fiiliCalismaCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, fiiliCalismaCol].Value)
                    : 0;

                var normalizedPerformans = DashboardParsingHelper.NormalizePercentValue(performans);
                var normalizedKayipSure = DashboardParsingHelper.NormalizePercentValue(kayipSure);
                var normalizedKullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(kullanilabilirlik);
                var normalizedKalite = DashboardParsingHelper.NormalizePercentValue(kalite);
                var normalizedOee = DashboardParsingHelper.NormalizePercentValue(oee);
                var normalizedFiiliCalisma = DashboardParsingHelper.NormalizePercentValue(fiiliCalisma);
                if (normalizedFiiliCalisma <= 0 && normalizedKullanilabilirlik > 0)
                {
                    normalizedFiiliCalisma = normalizedKullanilabilirlik;
                }

                result.Add(new PvcSatirModel
                {
                    Tarih = parsedDate,
                    Makine = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                    UretimMetraj = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 3].Value),
                    ParcaSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 4].Value),
                    CalismaKosulu = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                    Duraklama1 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 6].Value),
                    DuraklamaNedeni1 = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                    Duraklama2 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 8].Value),
                    DuraklamaNedeni2 = worksheet.Cells[row, 9].Value?.ToString()?.Trim(),
                    Duraklama3 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 10].Value),
                    DuraklamaNedeni3 = worksheet.Cells[row, 11].Value?.ToString()?.Trim(),
                    HataliParca = hataliParca,
                    Aciklama = aciklama,
                    Performans = normalizedPerformans,
                    UretimOrani = normalizedPerformans,
                    KayipSure = normalizedKayipSure,
                    Kullanilabilirlik = normalizedKullanilabilirlik,
                    Kalite = normalizedKalite,
                    Oee = normalizedOee,
                    FiiliCalismaOrani = normalizedFiiliCalisma
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PVC satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<MasterwoodSatirModel> ParseMasterwoodRows(string excelRoot)
    {
        var result = new List<MasterwoodSatirModel>();
        var fileCandidates = new[]
        {
            "MARWOOD Masterwood Veri Ekranı 2026.xlsm",
            "MARWOOD Masterwood Veri Ekranı.xlsm"
        };
        var filePath = fileCandidates
            .Select(name => Path.Combine(excelRoot, name))
            .FirstOrDefault(File.Exists);
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets
            .FirstOrDefault(ws =>
                ws.Name.Equals("GİRDİ RAPORU", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.Equals("GIRDI RAPORU", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.Equals("ANA RAPOR", StringComparison.OrdinalIgnoreCase));
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colCount = worksheet.Dimension.Columns;
        int duraklama2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 2 (DK)", "DURAKLAMA ZAMANI 2", "DURAKLAMA2");
        int duraklamaNeden2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 2", "DURAKLAMA NEDENI 2");
        int duraklama3Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 3 (DK)", "DURAKLAMA ZAMANI 3", "DURAKLAMA3");
        int duraklamaNeden3Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 3", "DURAKLAMA NEDENI 3");
        int hataliParcaCol = DashboardParsingHelper.FindColumn(worksheet, "HATALI PARÇA (MASTERWOOD)", "HATALI PARCA (MASTERWOOD)", "HATALI PARÇA", "HATALI PARCA");
        int aciklamaCol = DashboardParsingHelper.FindColumn(worksheet, "AÇIKLAMA", "ACIKLAMA");
        int performansCol = DashboardParsingHelper.FindColumn(worksheet, "PERFORMANS");
        int uretimOraniCol = DashboardParsingHelper.FindColumn(worksheet, "ÜRETİM ORANI", "URETIM ORANI", "ÜRETİMORANI", "URETIMORANI");
        int kayipSureCol = DashboardParsingHelper.FindColumn(worksheet, "KAYIP SÜRE", "KAYIP SURE", "KAYIPSURE");
        int kullanilabilirlikCol = DashboardParsingHelper.FindColumn(worksheet, "KULLANILABİLİRLİK", "KULLANILABILIRLIK");
        int kaliteCol = DashboardParsingHelper.FindColumn(worksheet, "KALİTE", "KALITE");
        int oeeCol = DashboardParsingHelper.FindColumn(worksheet, "OEE");
        int fiiliCalismaCol = DashboardParsingHelper.FindColumn(worksheet, "FİİLİ ÇALIŞMA ORANI", "FIILI CALISMA ORANI", "FIILI CALISMAORANI", "FİİLİ ÇALIŞMA");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                var duraklama2 = duraklama2Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama2Col].Value)
                    : (colCount >= 8 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 8].Value) : 0);
                var duraklamaNeden2 = duraklamaNeden2Col > 0
                    ? worksheet.Cells[row, duraklamaNeden2Col].Value?.ToString()?.Trim()
                    : (colCount >= 9 ? worksheet.Cells[row, 9].Value?.ToString()?.Trim() : null);
                var duraklama3 = duraklama3Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama3Col].Value)
                    : (colCount >= 10 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 10].Value) : 0);
                var duraklamaNeden3 = duraklamaNeden3Col > 0
                    ? worksheet.Cells[row, duraklamaNeden3Col].Value?.ToString()?.Trim()
                    : (colCount >= 11 ? worksheet.Cells[row, 11].Value?.ToString()?.Trim() : null);

                var hataliParca = hataliParcaCol > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, hataliParcaCol].Value)
                    : (colCount >= 12 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 12].Value) : 0);
                var aciklama = aciklamaCol > 0
                    ? worksheet.Cells[row, aciklamaCol].Value?.ToString()?.Trim()
                    : (colCount >= 13 ? worksheet.Cells[row, 13].Value?.ToString()?.Trim() : null);
                var performans = performansCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, performansCol].Value)
                    : (colCount >= 14 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 14].Value) : (uretimOraniCol > 0 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, uretimOraniCol].Value) : 0));
                var kayipSure = kayipSureCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kayipSureCol].Value)
                    : (colCount >= 15 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 15].Value) : 0);
                var kullanilabilirlik = kullanilabilirlikCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kullanilabilirlikCol].Value)
                    : (colCount >= 16 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 16].Value) : 0);
                var kalite = kaliteCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kaliteCol].Value)
                    : (colCount >= 17 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 17].Value) : 0);
                var oee = oeeCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, oeeCol].Value)
                    : (colCount >= 18 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 18].Value) : 0);
                var fiiliCalisma = fiiliCalismaCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, fiiliCalismaCol].Value)
                    : 0;

                var normalizedPerformans = DashboardParsingHelper.NormalizePercentValue(performans);
                var normalizedKayipSure = DashboardParsingHelper.NormalizePercentValue(kayipSure);
                var normalizedKullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(kullanilabilirlik);
                var normalizedKalite = DashboardParsingHelper.NormalizePercentValue(kalite);
                var normalizedOee = DashboardParsingHelper.NormalizePercentValue(oee);
                var normalizedFiiliCalisma = DashboardParsingHelper.NormalizePercentValue(fiiliCalisma);
                if (normalizedFiiliCalisma <= 0 && normalizedKullanilabilirlik > 0)
                {
                    normalizedFiiliCalisma = normalizedKullanilabilirlik;
                }

                result.Add(new MasterwoodSatirModel
                {
                    Tarih = parsedDate,
                    KisiSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 2].Value),
                    DelikSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 3].Value),
                    DelikFreezeSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 4].Value),
                    CalismaKosulu = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                    Duraklama1 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 6].Value),
                    DuraklamaNedeni1 = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                    Duraklama2 = duraklama2,
                    DuraklamaNedeni2 = duraklamaNeden2,
                    Duraklama3 = duraklama3,
                    DuraklamaNedeni3 = duraklamaNeden3,
                    HataliParca = hataliParca,
                    Aciklama = aciklama,
                    Performans = normalizedPerformans,
                    UretimOrani = normalizedPerformans,
                    KayipSureOrani = normalizedKayipSure,
                    Kullanilabilirlik = normalizedKullanilabilirlik,
                    Kalite = normalizedKalite,
                    Oee = normalizedOee,
                    FiiliCalismaOrani = normalizedFiiliCalisma
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Masterwood satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<SkipperSatirModel> ParseSkipperRows(string excelRoot)
    {
        var result = new List<SkipperSatirModel>();
        var fileCandidates = new[]
        {
            "MARWOOD Skipper Veri Ekranı 2026.xlsm",
            "MARWOOD Skipper Veri Ekranı düzeltilmiş.xlsm",
            "MARWOOD Skipper Veri Ekranı düzeltilmiş.xlsm"
        };
        var filePath = fileCandidates
            .Select(name => Path.Combine(excelRoot, name))
            .FirstOrDefault(File.Exists);
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets
            .FirstOrDefault(ws =>
                ws.Name.Equals("GİRDİ RAPORU", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.Equals("GIRDI RAPORU", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.Equals("ANA RAPOR", StringComparison.OrdinalIgnoreCase));
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colCount = worksheet.Dimension.Columns;
        int duraklama2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 2 (DK)", "DURAKLAMA ZAMANI 2", "DURAKLAMA2");
        int duraklamaNeden2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 2", "DURAKLAMA NEDENI 2");
        int duraklama3Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 3 (DK)", "DURAKLAMA ZAMANI 3", "DURAKLAMA3");
        int duraklamaNeden3Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 3", "DURAKLAMA NEDENI 3");
        int hataliParcaCol = DashboardParsingHelper.FindColumn(worksheet, "HATALI PARÇA", "HATALI PARCA");
        int aciklamaCol = DashboardParsingHelper.FindColumn(worksheet, "AÇIKLAMA", "ACIKLAMA");
        int performansCol = DashboardParsingHelper.FindColumn(worksheet, "PERFORMANS");
        int uretimOraniCol = DashboardParsingHelper.FindColumn(worksheet, "ÜRETİM ORANI", "URETIM ORANI", "ÜRETİMORANI", "URETIMORANI");
        int kayipSureCol = DashboardParsingHelper.FindColumn(worksheet, "KAYIP SÜRE ORANI", "KAYIP SURE ORANI", "KAYIP SÜRE", "KAYIP SURE");
        int kullanilabilirlikCol = DashboardParsingHelper.FindColumn(worksheet, "KULLANILABİLİRLİK", "KULLANILABILIRLIK");
        int kaliteCol = DashboardParsingHelper.FindColumn(worksheet, "KALİTE", "KALITE");
        int oeeCol = DashboardParsingHelper.FindColumn(worksheet, "OEE");
        int fiiliCalismaCol = DashboardParsingHelper.FindColumn(worksheet, "FİİLİ ÇALIŞMA ORANI", "FIILI CALISMA ORANI", "FIILI CALISMAORANI", "FİİLİ ÇALIŞMA");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                var duraklama2 = duraklama2Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama2Col].Value)
                    : (colCount >= 7 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 7].Value) : 0);
                var duraklamaNeden2 = duraklamaNeden2Col > 0
                    ? worksheet.Cells[row, duraklamaNeden2Col].Value?.ToString()?.Trim()
                    : (colCount >= 8 ? worksheet.Cells[row, 8].Value?.ToString()?.Trim() : null);
                var duraklama3 = duraklama3Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama3Col].Value)
                    : (colCount >= 9 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 9].Value) : 0);
                var duraklamaNeden3 = duraklamaNeden3Col > 0
                    ? worksheet.Cells[row, duraklamaNeden3Col].Value?.ToString()?.Trim()
                    : (colCount >= 10 ? worksheet.Cells[row, 10].Value?.ToString()?.Trim() : null);

                var hataliParca = hataliParcaCol > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, hataliParcaCol].Value)
                    : (colCount >= 11 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 11].Value) : 0);
                var aciklama = aciklamaCol > 0
                    ? worksheet.Cells[row, aciklamaCol].Value?.ToString()?.Trim()
                    : (colCount >= 12 ? worksheet.Cells[row, 12].Value?.ToString()?.Trim() : null);
                var performans = performansCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, performansCol].Value)
                    : (colCount >= 13 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 13].Value) : (uretimOraniCol > 0 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, uretimOraniCol].Value) : 0));
                var kayipSure = kayipSureCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kayipSureCol].Value)
                    : (colCount >= 14 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 14].Value) : 0);
                var kullanilabilirlik = kullanilabilirlikCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kullanilabilirlikCol].Value)
                    : (colCount >= 15 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 15].Value) : 0);
                var kalite = kaliteCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kaliteCol].Value)
                    : (colCount >= 16 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 16].Value) : 0);
                var oee = oeeCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, oeeCol].Value)
                    : (colCount >= 17 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 17].Value) : 0);
                var fiiliCalisma = fiiliCalismaCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, fiiliCalismaCol].Value)
                    : 0;

                var normalizedPerformans = DashboardParsingHelper.NormalizePercentValue(performans);
                var normalizedKayipSure = DashboardParsingHelper.NormalizePercentValue(kayipSure);
                var normalizedKullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(kullanilabilirlik);
                var normalizedKalite = DashboardParsingHelper.NormalizePercentValue(kalite);
                var normalizedOee = DashboardParsingHelper.NormalizePercentValue(oee);
                var normalizedFiiliCalisma = DashboardParsingHelper.NormalizePercentValue(fiiliCalisma);
                if (normalizedFiiliCalisma <= 0 && normalizedKullanilabilirlik > 0)
                {
                    normalizedFiiliCalisma = normalizedKullanilabilirlik;
                }

                result.Add(new SkipperSatirModel
                {
                    Tarih = parsedDate,
                    KisiSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 2].Value),
                    DelikSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 3].Value),
                    CalismaKosulu = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                    Duraklama1 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 5].Value),
                    DuraklamaNedeni1 = worksheet.Cells[row, 6].Value?.ToString()?.Trim(),
                    Duraklama2 = duraklama2,
                    DuraklamaNedeni2 = duraklamaNeden2,
                    Duraklama3 = duraklama3,
                    DuraklamaNedeni3 = duraklamaNeden3,
                    HataliParca = hataliParca,
                    Aciklama = aciklama,
                    Performans = normalizedPerformans,
                    UretimOrani = normalizedPerformans,
                    KayipSureOrani = normalizedKayipSure,
                    Kullanilabilirlik = normalizedKullanilabilirlik,
                    Kalite = normalizedKalite,
                    Oee = normalizedOee,
                    FiiliCalismaOrani = normalizedFiiliCalisma
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipper satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<RoverBSatirModel> ParseRoverBRows(string excelRoot)
    {
        var result = new List<RoverBSatirModel>();
        var fileCandidates = new[]
        {
            "MARWOOD Rover-B Veri Ekranı 2026.xlsm",
            "MARWOOD Rover-B Veri Ekranı.xlsm"
        };
        var filePath = fileCandidates
            .Select(name => Path.Combine(excelRoot, name))
            .FirstOrDefault(File.Exists);
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets
            .FirstOrDefault(ws =>
                ws.Name.Equals("GİRDİ RAPORU", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.Equals("GIRDI RAPORU", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.Equals("ANA RAPOR", StringComparison.OrdinalIgnoreCase));
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colCount = worksheet.Dimension.Columns;
        int duraklama2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 2 (DK)", "DURAKLAMA ZAMANI 2", "DURAKLAMA2");
        int duraklamaNeden2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 2", "DURAKLAMA NEDENI 2");
        int duraklama3Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 3 (DK)", "DURAKLAMA ZAMANI 3", "DURAKLAMA3");
        int duraklamaNeden3Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 3", "DURAKLAMA NEDENI 3");
        int duraklama4Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI 4 (DK)", "DURAKLAMA ZAMANI 4", "DURAKLAMA4");
        int duraklamaNeden4Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 4", "DURAKLAMA NEDENI 4");
        int hataliParcaCol = DashboardParsingHelper.FindColumn(worksheet, "HATALI PARÇA SAYISI", "HATALI PARCA SAYISI", "HATALI PARÇA", "HATALI PARCA");
        int aciklamaCol = DashboardParsingHelper.FindColumn(worksheet, "AÇIKLAMA", "ACIKLAMA");
        int performansCol = DashboardParsingHelper.FindColumn(worksheet, "PERFORMANS", "PERFONMANS");
        int kayipSureCol = DashboardParsingHelper.FindColumn(worksheet, "KAYIP SÜRE", "KAYIP SURE", "KAYIPSURE");
        int kullanilabilirlikCol = DashboardParsingHelper.FindColumn(worksheet, "KULLANILABİLİRLİK", "KULLANILABILIRLIK");
        int kaliteCol = DashboardParsingHelper.FindColumn(worksheet, "KALİTE ORANI", "KALITE ORANI", "KALİTE", "KALITE");
        int oeeCol = DashboardParsingHelper.FindColumn(worksheet, "OEE");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                var duraklama2 = duraklama2Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama2Col].Value)
                    : (colCount >= 8 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 8].Value) : 0);
                var duraklamaNeden2 = duraklamaNeden2Col > 0
                    ? worksheet.Cells[row, duraklamaNeden2Col].Value?.ToString()?.Trim()
                    : (colCount >= 9 ? worksheet.Cells[row, 9].Value?.ToString()?.Trim() : null);
                var duraklama3 = duraklama3Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama3Col].Value)
                    : (colCount >= 10 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 10].Value) : 0);
                var duraklamaNeden3 = duraklamaNeden3Col > 0
                    ? worksheet.Cells[row, duraklamaNeden3Col].Value?.ToString()?.Trim()
                    : (colCount >= 11 ? worksheet.Cells[row, 11].Value?.ToString()?.Trim() : null);
                var duraklama4 = duraklama4Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama4Col].Value)
                    : (colCount >= 12 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 12].Value) : 0);
                var duraklamaNeden4 = duraklamaNeden4Col > 0
                    ? worksheet.Cells[row, duraklamaNeden4Col].Value?.ToString()?.Trim()
                    : (colCount >= 13 ? worksheet.Cells[row, 13].Value?.ToString()?.Trim() : null);

                var hataliParca = hataliParcaCol > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, hataliParcaCol].Value)
                    : (colCount >= 14 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 14].Value) : 0);
                var aciklama = aciklamaCol > 0
                    ? worksheet.Cells[row, aciklamaCol].Value?.ToString()?.Trim()
                    : (colCount >= 15 ? worksheet.Cells[row, 15].Value?.ToString()?.Trim() : null);
                var performans = performansCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, performansCol].Value)
                    : (colCount >= 16 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 16].Value) : 0);
                var kayipSure = kayipSureCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kayipSureCol].Value)
                    : (colCount >= 17 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 17].Value) : 0);
                var kullanilabilirlik = kullanilabilirlikCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kullanilabilirlikCol].Value)
                    : (colCount >= 18 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 18].Value) : 0);
                var kalite = kaliteCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kaliteCol].Value)
                    : (colCount >= 19 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 19].Value) : 0);
                var oee = oeeCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, oeeCol].Value)
                    : (colCount >= 20 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 20].Value) : 0);

                var normalizedPerformans = DashboardParsingHelper.NormalizePercentValue(performans);
                var normalizedKayipSure = DashboardParsingHelper.NormalizePercentValue(kayipSure);
                var normalizedKullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(kullanilabilirlik);
                var normalizedKalite = DashboardParsingHelper.NormalizePercentValue(kalite);
                var normalizedOee = DashboardParsingHelper.NormalizePercentValue(oee);

                result.Add(new RoverBSatirModel
                {
                    Tarih = parsedDate,
                    KisiSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 2].Value),
                    DelikFreezeSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 3].Value),
                    DelikFreezePvcSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 4].Value),
                    CalismaKosulu = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                    Duraklama1 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 6].Value),
                    DuraklamaNedeni1 = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                    Duraklama2 = duraklama2,
                    DuraklamaNedeni2 = duraklamaNeden2,
                    Duraklama3 = duraklama3,
                    DuraklamaNedeni3 = duraklamaNeden3,
                    Duraklama4 = duraklama4,
                    DuraklamaNedeni4 = duraklamaNeden4,
                    HataliParca = hataliParca,
                    Aciklama = aciklama,
                    Performans = normalizedPerformans,
                    UretimOrani = normalizedPerformans,
                    KayipSureOrani = normalizedKayipSure,
                    Kullanilabilirlik = normalizedKullanilabilirlik,
                    Kalite = normalizedKalite,
                    Oee = normalizedOee,
                    FiiliCalismaOrani = normalizedKullanilabilirlik
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rover-B satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<TezgahSatirModel> ParseTezgahRows(string excelRoot)
    {
        var result = new List<TezgahSatirModel>();
        var filePath = Path.Combine(excelRoot, "MARWOOD Tezgah Bölümü Veri Ekranı.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets["ANA RAPOR"];
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colCount = worksheet.Dimension.Columns;
        int kullanilabilirlikCol = DashboardParsingHelper.FindColumn(worksheet, "KULLANILABİLİRLİK", "KULLANILABILIRLIK");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                var kullanilabilirlik = kullanilabilirlikCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kullanilabilirlikCol].Value)
                    : (colCount >= 10 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 10].Value) : 0);

                result.Add(new TezgahSatirModel
                {
                    Tarih = parsedDate,
                    TezgahUrunleri = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                    KisiSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 3].Value),
                    ParcaAdeti = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 4].Value),
                    SureDakika = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 5].Value),
                    CalismaKosulu = worksheet.Cells[row, 6].Value?.ToString()?.Trim(),
                    KayipSureNedeni = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                    KayipSureDakika = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 8].Value),
                    Aciklama = worksheet.Cells[row, 9].Value?.ToString()?.Trim(),
                    Kullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(kullanilabilirlik)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tezgah satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<EbatlamaSatirModel> ParseEbatlamaRows(string excelRoot)
    {
        var result = new List<EbatlamaSatirModel>();
        var fileCandidates = new[]
        {
            "EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm",
            "EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm",
            "EBATLAMA BÖLÜMÜ VERİ EKRANI.xlsm",
            "EBATLAMA BÖLÜMÜ VERİ EKRANI.xlsm"
        };
        var filePath = fileCandidates
            .Select(name => Path.Combine(excelRoot, name))
            .FirstOrDefault(File.Exists);
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets["KAYIT"];
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colCount = worksheet.Dimension.Columns;
        int duraklama1Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI (DK) 1", "DURAKLAMA ZAMANI 1 (DK)", "DURAKLAMA ZAMANI 1");
        int duraklamaNeden1Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 1", "DURAKLAMA NEDENI 1");
        int duraklama2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA ZAMANI (DK) 2", "DURAKLAMA ZAMANI 2 (DK)", "DURAKLAMA ZAMANI 2");
        int duraklamaNeden2Col = DashboardParsingHelper.FindColumn(worksheet, "DURAKLAMA NEDENİ 2", "DURAKLAMA NEDENI 2");
        int hazirlikCol = DashboardParsingHelper.FindColumn(worksheet, "HAZIRLIK / MALZEME TASIMA (DK)", "HAZIRLIK / MALZEME TAŞIMA (DK)", "HAZIRLIK MALZEME TASIMA");
        int performansCol = DashboardParsingHelper.FindColumn(worksheet, "PERFORMANS");
        int kullanilabilirlikCol = DashboardParsingHelper.FindColumn(worksheet, "KULLANILABİLİRLİK", "KULLANILABILIRLIK");
        int kaliteCol = DashboardParsingHelper.FindColumn(worksheet, "KALİTE", "KALITE");
        int oeeCol = DashboardParsingHelper.FindColumn(worksheet, "OEE");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var dateCell = worksheet.Cells[row, 1];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                var duraklama1 = duraklama1Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama1Col].Value)
                    : (colCount >= 12 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 12].Value) : 0);
                var duraklamaNedeni1 = duraklamaNeden1Col > 0
                    ? worksheet.Cells[row, duraklamaNeden1Col].Value?.ToString()?.Trim()
                    : (colCount >= 13 ? worksheet.Cells[row, 13].Value?.ToString()?.Trim() : null);
                var duraklama2 = duraklama2Col > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, duraklama2Col].Value)
                    : (colCount >= 14 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 14].Value) : 0);
                var duraklamaNedeni2 = duraklamaNeden2Col > 0
                    ? worksheet.Cells[row, duraklamaNeden2Col].Value?.ToString()?.Trim()
                    : (colCount >= 15 ? worksheet.Cells[row, 15].Value?.ToString()?.Trim() : null);
                var hazirlik = hazirlikCol > 0
                    ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, hazirlikCol].Value)
                    : (colCount >= 11 ? DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 11].Value) : 0);
                var performans = performansCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, performansCol].Value)
                    : (colCount >= 17 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 17].Value) : 0);
                var kullanilabilirlik = kullanilabilirlikCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kullanilabilirlikCol].Value)
                    : (colCount >= 18 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 18].Value) : 0);
                var kalite = kaliteCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, kaliteCol].Value)
                    : (colCount >= 19 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 19].Value) : 0);
                var oee = oeeCol > 0
                    ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, oeeCol].Value)
                    : (colCount >= 20 ? DashboardParsingHelper.ParsePercentCell(worksheet.Cells[row, 20].Value) : 0);

                result.Add(new EbatlamaSatirModel
                {
                    Tarih = parsedDate,
                    Makine = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                    Plaka8Mm = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 3].Value),
                    Kesim8MmAdet = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 4].Value),
                    Plaka18Mm = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 5].Value),
                    Plaka30Mm = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 6].Value),
                    Kesim30MmAdet = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 7].Value),
                    ToplamKesimAdet = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 8].Value),
                    Gonyelleme = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 9].Value),
                    MesaiDurumu = worksheet.Cells[row, 10].Value?.ToString()?.Trim(),
                    HazirlikMalzemeDakika = hazirlik,
                    Duraklama1 = duraklama1,
                    DuraklamaNedeni1 = duraklamaNedeni1,
                    Duraklama2 = duraklama2,
                    DuraklamaNedeni2 = duraklamaNedeni2,
                    Performans = DashboardParsingHelper.NormalizePercentValue(performans),
                    Kullanilabilirlik = DashboardParsingHelper.NormalizePercentValue(kullanilabilirlik),
                    Kalite = DashboardParsingHelper.NormalizePercentValue(kalite),
                    Oee = DashboardParsingHelper.NormalizePercentValue(oee)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ebatlama satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }

    private List<PersonelYoklamaSatirModel> ParsePersonelRows(string excelRoot)
    {
        var result = new List<PersonelYoklamaSatirModel>();
        var fileCandidates = new[]
        {
            "Günlük Personel Sayısı.xlsm",
            "Günlük Personel Sayısı (1).xlsm",
            "Günlük Personel Sayısı (1).xlsm"
        };
        var filePath = fileCandidates
            .Select(name => Path.Combine(excelRoot, name))
            .FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets
            .FirstOrDefault(ws => ws.Name.Equals("YOKLAMA TABLOSU", StringComparison.OrdinalIgnoreCase));
        if (worksheet?.Dimension == null)
        {
            return result;
        }

        int colTarih = DashboardParsingHelper.FindColumn(worksheet, "TARİH", "TARIH");
        int colBolum = DashboardParsingHelper.FindColumn(worksheet, "BÖLÜM", "BOLUM", "BÖLÜM ADI", "BOLUM ADI");
        int colPersonelSayisi = DashboardParsingHelper.FindColumn(worksheet, "PERSONEL SAYISI", "PERSONEL");
        int colAciklama = DashboardParsingHelper.FindColumn(worksheet, "AÇIKLAMA", "ACIKLAMA");

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                int tarihCol = colTarih > 0 ? colTarih : 1;
                int bolumCol = colBolum > 0 ? colBolum : 2;
                int personelCol = colPersonelSayisi > 0 ? colPersonelSayisi : 3;
                int aciklamaCol = colAciklama > 0 ? colAciklama : 4;

                var dateCell = worksheet.Cells[row, tarihCol];
                var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);
                var bolumAdi = worksheet.Cells[row, bolumCol].Value?.ToString()?.Trim();
                var personelSayisi = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, personelCol].Value);
                var aciklama = worksheet.Cells[row, aciklamaCol].Value?.ToString()?.Trim();

                if (parsedDate == DateTime.MinValue && string.IsNullOrWhiteSpace(bolumAdi) && personelSayisi == 0)
                {
                    continue;
                }

                result.Add(new PersonelYoklamaSatirModel
                {
                    Tarih = parsedDate,
                    BolumAdi = bolumAdi,
                    PersonelSayisi = personelSayisi,
                    Aciklama = aciklama
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Personel yoklama satırı parse edilemedi. Satır: {Row}", row);
            }
        }

        return result
            .Where(x => x.Tarih != DateTime.MinValue && !string.IsNullOrWhiteSpace(x.BolumAdi))
            .ToList();
    }

    private List<HataliParcaSatirModel> ParseHataliParcaRows(string excelRoot)
    {
        var result = new List<HataliParcaSatirModel>();
        var filePath = Path.Combine(excelRoot, "HATALI PARÇA VERİ GİRİŞİ.xlsm");
        if (!File.Exists(filePath))
        {
            return result;
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var sheetNames = new[] { "VERİ KAYIT", "2 ocak-24 ekim SAYFASI" };
        var worksheets = sheetNames
            .Select(name => package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(ws => ws?.Dimension != null)
            .Cast<ExcelWorksheet>()
            .ToList();

        foreach (var worksheet in worksheets)
        {
            int colTarih = DashboardParsingHelper.FindColumn(worksheet, "TARİH", "TARIH");
            int colBolum = DashboardParsingHelper.FindColumn(worksheet, "BÖLÜM ADI", "BOLUM ADI");
            int colTalepAcan = DashboardParsingHelper.FindColumn(worksheet, "TALEP AÇAN KULLANICI", "TALEP ACAN KULLANICI");
            int colSiparis = DashboardParsingHelper.FindColumn(worksheet, "SİPARİŞ NO - SIRA NO", "SIPARIS NO - SIRA NO", "SIPARIS NO");
            int colUrun = DashboardParsingHelper.FindColumn(worksheet, "ÜRÜN İSMİ", "URUN ISMI");
            int colRenk = DashboardParsingHelper.FindColumn(worksheet, "RENK");
            int colKalinlik = DashboardParsingHelper.FindColumn(worksheet, "KALINLIK");
            int colBoy = DashboardParsingHelper.FindColumn(worksheet, "BOY");
            int colEn = DashboardParsingHelper.FindColumn(worksheet, "EN");
            int colAdet = DashboardParsingHelper.FindColumn(worksheet, "ADET");
            int colM2 = DashboardParsingHelper.FindColumn(worksheet, "TOPLAM M2", "TOPLAM M²");
            int colHata = DashboardParsingHelper.FindColumn(worksheet, "HATA NEDENİ", "HATA NEDENI");
            int colOperator = DashboardParsingHelper.FindColumn(worksheet, "PARÇAYI İŞLEYEN OPERATÖR ADI", "PARCAYI ISLEYEN OPERATOR ADI", "OPERATÖR ADI", "OPERATOR ADI");
            int colKesim = DashboardParsingHelper.FindColumn(worksheet, "KESİM DURUMU", "KESIM DURUMU");
            int colPvc = DashboardParsingHelper.FindColumn(worksheet, "PVC DURUMU");

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                try
                {
                    int tarihCol = colTarih > 0 ? colTarih : 1;
                    var dateCell = worksheet.Cells[row, tarihCol];
                    var parsedDate = DashboardParsingHelper.ParseDateCell(dateCell.Value, dateCell.Text);

                    int bolumCol = colBolum > 0 ? colBolum : 2;
                    int talepCol = colTalepAcan > 0 ? colTalepAcan : 3;
                    int siparisCol = colSiparis > 0 ? colSiparis : 4;
                    int urunCol = colUrun > 0 ? colUrun : 5;
                    int renkCol = colRenk > 0 ? colRenk : 6;
                    int kalinlikCol = colKalinlik > 0 ? colKalinlik : 7;
                    int boyCol = colBoy > 0 ? colBoy : 8;
                    int enCol = colEn > 0 ? colEn : 9;
                    int adetCol = colAdet > 0 ? colAdet : 10;
                    int m2Col = colM2 > 0 ? colM2 : 11;
                    int hataCol = colHata > 0 ? colHata : 12;
                    int operatorCol = colOperator > 0 ? colOperator : 13;
                    int kesimCol = colKesim > 0 ? colKesim : 14;
                    int pvcCol = colPvc > 0 ? colPvc : 15;

                    result.Add(new HataliParcaSatirModel
                    {
                        Tarih = parsedDate,
                        BolumAdi = worksheet.Cells[row, bolumCol].Value?.ToString()?.Trim(),
                        TalepAcanKullanici = worksheet.Cells[row, talepCol].Value?.ToString()?.Trim(),
                        SiparisNo = worksheet.Cells[row, siparisCol].Value?.ToString()?.Trim(),
                        UrunIsmi = worksheet.Cells[row, urunCol].Value?.ToString()?.Trim(),
                        Renk = worksheet.Cells[row, renkCol].Value?.ToString()?.Trim(),
                        Kalinlik = worksheet.Cells[row, kalinlikCol].Value?.ToString()?.Trim(),
                        Boy = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, boyCol].Value),
                        En = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, enCol].Value),
                        Adet = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, adetCol].Value),
                        ToplamM2 = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, m2Col].Value),
                        HataNedeni = worksheet.Cells[row, hataCol].Value?.ToString()?.Trim(),
                        OperatorAdi = worksheet.Cells[row, operatorCol].Value?.ToString()?.Trim(),
                        KesimDurumu = worksheet.Cells[row, kesimCol].Value?.ToString()?.Trim(),
                        PvcDurumu = worksheet.Cells[row, pvcCol].Value?.ToString()?.Trim()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Hatalı Parça satırı parse edilemedi. Sayfa: {Sheet}, Satır: {Row}", worksheet.Name, row);
                }
            }
        }

        return result.Where(x => x.Tarih != DateTime.MinValue).ToList();
    }
}
