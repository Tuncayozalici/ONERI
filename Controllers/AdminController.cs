using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Models;
using Microsoft.AspNetCore.Authorization;
using ONERI.Models.Authorization;
using OfficeOpenXml;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using ONERI.Services.Dashboards;
using System.Text;
using System.Globalization;

namespace ONERI.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FabrikaContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IDashboardIngestionService _dashboardIngestionService;
        private readonly ILogger<AdminController> _logger;
        private static readonly string[] GunlukCalismaDosyaAdaylari =
        {
            "Günlük Çalışma Verileri.xlsx",
            "Günlük Çalışma Verileri 1.xlsx",
            "Günlük Çalışma Verileri 1.xlsx"
        };

        private static readonly (string BolumAnahtari, string BolumGosterimAdi, string? EkLabel)[] GunlukRaporBolumSirasi =
        {
            ("KESIM", "KESIM", "Ahsap Renk Cesitliligi"),
            ("PVC", "PVC", "PVC Renk Cesitliligi"),
            ("BOYA", "BOYA", "Boya Renk Cesitliligi"),
            ("METAL", "METAL", null),
            ("CNC", "CNC", null),
            ("KESON", "KESON", null),
            ("MONTAJ", "MONTAJ", null),
            ("SAC LAZER", "SAC LAZER", null)
        };

        public AdminController(
            FabrikaContext context,
            IWebHostEnvironment hostingEnvironment,
            IDashboardIngestionService dashboardIngestionService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _dashboardIngestionService = dashboardIngestionService;
            _logger = logger;
        }

        // Görev 1: Listeleme (Index Metodu)
        [Authorize(Policy = Permissions.OneriAdmin.Access)]
        public async Task<IActionResult> Index(string durum, string arama)
        {
            // Başlangıç sorgusu - veritabanından veri çekilmez.
            var onerilerSorgusu = _context.Oneriler.OrderByDescending(o => o.Tarih).AsQueryable();

            // Duruma göre filtreleme
            if (!string.IsNullOrEmpty(durum) && durum != "Tümü")
            {
                switch (durum)
                {
                    case "Onaylanan":
                        onerilerSorgusu = onerilerSorgusu.Where(o => o.Durum == OneriDurum.Onaylandi);
                        break;
                    case "Bekleyen":
                        onerilerSorgusu = onerilerSorgusu.Where(o => o.Durum == OneriDurum.Beklemede);
                        break;
                    case "Reddedilen":
                        onerilerSorgusu = onerilerSorgusu.Where(o => o.Durum == OneriDurum.Reddedildi);
                        break;
                }
            }

            // Arama kelimesine göre filtreleme
            if (!string.IsNullOrEmpty(arama))
            {
                var aramaLower = arama.ToLower();
                onerilerSorgusu = onerilerSorgusu.Where(o => 
                    o.Konu.ToLower().Contains(aramaLower) || 
                    o.Aciklama.ToLower().Contains(aramaLower));
            }
    
            // View'e göndermeden hemen önce sorguyu çalıştırıp listeyi al.
            var sonuclar = await onerilerSorgusu.ToListAsync();

            ViewData["MevcutDurum"] = durum;
            ViewData["MevcutArama"] = arama;

            return View(sonuclar);
        }

        // Görev 2: Onaylama (Onayla Metodu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.OneriAdmin.Access)]
        [Authorize(Policy = Permissions.OneriAdmin.Approve)]
        public async Task<IActionResult> Onayla(int id)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                return NotFound();
            }

            oneri.Durum = OneriDurum.Onaylandi;
            _context.Update(oneri);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Görev 3: Reddetme (Reddet Metodu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.OneriAdmin.Access)]
        [Authorize(Policy = Permissions.OneriAdmin.Reject)]
        public async Task<IActionResult> Reddet(int id)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                return NotFound();
            }

            oneri.Durum = OneriDurum.Reddedildi;
            _context.Update(oneri);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        #region Bölüm Yöneticisi Yönetimi

        // A. Listeleme (Rehberi Göster)
        [HttpGet]
        [Authorize(Policy = Permissions.BolumYoneticileri.View)]
        public async Task<IActionResult> BolumYoneticileri()
        {
            var yoneticiler = await _context.BolumYoneticileri.OrderBy(y => y.BolumAdi).ToListAsync();
            // The form will be on this view, so we can pass a new BolumYonetici model for it.
            ViewBag.YeniYonetici = new BolumYonetici();
            return View(yoneticiler);
        }

        // B. Yeni Sorumlu Ekleme (GET)
        [HttpGet]
        [Authorize(Policy = Permissions.BolumYoneticileri.Create)]
        public IActionResult YoneticiEkle()
        {
            return View();
        }

        // B. Yeni Sorumlu Ekleme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.BolumYoneticileri.Create)]
        public async Task<IActionResult> YoneticiEkle(BolumYonetici bolumYonetici)
        {
            if (ModelState.IsValid)
            {
                // Null kontrolü ile güvenli karşılaştırma
                var existing = await _context.BolumYoneticileri
                    .FirstOrDefaultAsync(y => y.BolumAdi.ToLower() == (bolumYonetici.BolumAdi ?? "").ToLower());
                
                if (existing != null)
                {
                    // Redirecting will lose the error message, but it's the simplest approach without a ViewModel.
                    // A TempData message could be used to show the error after redirection.
                    TempData["YoneticiHata"] = "Bu bölüm için zaten bir yönetici atanmış.";
                    return RedirectToAction(nameof(BolumYoneticileri));
                }

                try
                {
                    _context.Add(bolumYonetici);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(BolumYoneticileri));
                }
                catch (DbUpdateException)
                {
                    TempData["YoneticiHata"] = "Bu bölüm için zaten bir yönetici atanmış.";
                    return RedirectToAction(nameof(BolumYoneticileri));
                }
            }

            // Hata durumunda formu kendi view'ında tekrar göster
            return View(bolumYonetici);
        }

        // C. Sorumlu Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.BolumYoneticileri.Delete)]
        public async Task<IActionResult> YoneticiSil(int id)
        {
            var yonetici = await _context.BolumYoneticileri.FindAsync(id);
            if (yonetici == null)
            {
                return NotFound();
            }

            // İlişkili öneri olup olmadığını kontrol et
            var iliskiliOneriVar = await _context.Oneriler.AnyAsync(o => o.Bolum == yonetici.BolumAdi);
            if (iliskiliOneriVar)
            {
                TempData["YoneticiHata"] = $"'{yonetici.BolumAdi}' bölümüne ait öneriler bulunduğu için yönetici silinemez.";
                return RedirectToAction(nameof(BolumYoneticileri));
            }

            _context.BolumYoneticileri.Remove(yonetici);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(BolumYoneticileri));
        }

        #endregion

        // Görev 4: Detay Sayfası
        [HttpGet]
        [Authorize(Policy = Permissions.OneriAdmin.Access)]
        [Authorize(Policy = Permissions.OneriAdmin.Detail)]
        public async Task<IActionResult> Detay(int id)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                return NotFound();
            }
            return View(oneri);
        }

        // Görev 5: Silme (Sil Metodu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.OneriAdmin.Access)]
        [Authorize(Policy = Permissions.OneriAdmin.Delete)]
        public async Task<IActionResult> Sil(int id)
        {
            var oneri = await _context.Oneriler.FindAsync(id);
            if (oneri == null)
            {
                return NotFound();
            }

            _context.Oneriler.Remove(oneri);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = Permissions.VeriYukle.Create)]
        public IActionResult GunlukVerileriIndir(DateTime? tarih, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil)
        {
            var dosyaYolu = ResolveGunlukCalismaDosyaYolu();
            if (string.IsNullOrWhiteSpace(dosyaYolu))
            {
                return NotFound("Günlük çalışma verisi dosyası bulunamadı. Önce Veri Yükle ekranından dosyayı yükleyin.");
            }

            GunlukVeriRaporModel? raporModeli;
            try
            {
                var gun = raporTarihi ?? tarih;
                raporModeli = HazirlaGunlukVeriRaporModeli(dosyaYolu, gun, baslangicTarihi, bitisTarihi, ay, yil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Günlük veriler PDF hazırlanırken hata oluştu.");
                return StatusCode(500, "Günlük veriler PDF hazırlanırken bir hata oluştu.");
            }

            if (raporModeli == null || raporModeli.Bolumler.Count == 0)
            {
                if (raporTarihi.HasValue || tarih.HasValue)
                {
                    var day = raporTarihi ?? tarih;
                    return NotFound($"{day:dd.MM.yyyy} tarihine ait günlük veri bulunamadı.");
                }

                return NotFound("Günlük veriler bulunamadı.");
            }

            var pdfIcerigi = BuildGunlukVeriStyledPdf(raporModeli);
            var dosyaAdi = BuildGunlukVeriPdfDosyaAdi(raporModeli);

            return File(pdfIcerigi, "application/pdf", dosyaAdi);
        }

        // Veri Yükle Sayfası
        [HttpGet]
        [Authorize(Policy = Permissions.VeriYukle.Create)]
        public IActionResult VeriYukle()
        {
            return View(new VeriYukleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.VeriYukle.Create)]
        public async Task<IActionResult> VeriYukle(VeriYukleViewModel model)
        {
            var results = new List<VeriYukleResult>();

            string rootPath = _hostingEnvironment.WebRootPath;
            string excelPath = Path.Combine(rootPath, "EXCELS");
            Directory.CreateDirectory(excelPath);

            var fileMap = new Dictionary<string, (string TargetPath, string SheetName)>(StringComparer.OrdinalIgnoreCase)
            {
                ["MARWOOD Profil Lazer Veri Ekranı.xlsm"] = (Path.Combine(excelPath, "MARWOOD Profil Lazer Veri Ekranı.xlsm"), "LAZER KAYIT"),
                ["YENİ BOYA GÜNLÜK VERİ TAKİP 2026 YILI.xlsm"] = (Path.Combine(excelPath, "YENİ BOYA GÜNLÜK VERİ TAKİP 2026 YILI.xlsm"), "VERİ KAYIT"),
                ["BOYA HATALI  PARÇA GİRİŞİ.xlsm"] = (Path.Combine(excelPath, "BOYA HATALI  PARÇA GİRİŞİ.xlsm"), "VERİ KAYIT"),
                ["PVC BÖLÜMÜ VERİ EKRANI 2026.xlsm"] = (Path.Combine(excelPath, "PVC BÖLÜMÜ VERİ EKRANI 2026.xlsm"), "KAYIT"),
                ["METAL HATALI  PARÇA GİRİŞİ.xlsm"] = (Path.Combine(excelPath, "METAL HATALI  PARÇA GİRİŞİ.xlsm"), "VERİ KAYIT"),
                ["MARWOOD Masterwood Veri Ekranı 2026.xlsm"] = (Path.Combine(excelPath, "MARWOOD Masterwood Veri Ekranı 2026.xlsm"), "GİRDİ RAPORU"),
                ["MARWOOD Masterwood Veri Ekranı.xlsm"] = (Path.Combine(excelPath, "MARWOOD Masterwood Veri Ekranı 2026.xlsm"), "GİRDİ RAPORU"),
                ["MARWOOD Skipper Veri Ekranı 2026.xlsm"] = (Path.Combine(excelPath, "MARWOOD Skipper Veri Ekranı 2026.xlsm"), "GİRDİ RAPORU"),
                ["MARWOOD Skipper Veri Ekranı düzeltilmiş.xlsm"] = (Path.Combine(excelPath, "MARWOOD Skipper Veri Ekranı 2026.xlsm"), "GİRDİ RAPORU"),
                ["MARWOOD Rover-B Veri Ekranı 2026.xlsm"] = (Path.Combine(excelPath, "MARWOOD Rover-B Veri Ekranı 2026.xlsm"), "GİRDİ RAPORU"),
                ["MARWOOD Rover-B Veri Ekranı.xlsm"] = (Path.Combine(excelPath, "MARWOOD Rover-B Veri Ekranı 2026.xlsm"), "GİRDİ RAPORU"),
                ["MARWOOD Tezgah Bölümü Veri Ekranı.xlsm"] = (Path.Combine(excelPath, "MARWOOD Tezgah Bölümü Veri Ekranı.xlsm"), "ANA RAPOR"),
                ["EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm"] = (Path.Combine(excelPath, "EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm"), "KAYIT"),
                ["EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm"] = (Path.Combine(excelPath, "EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm"), "KAYIT"),
                ["EBATLAMA BÖLÜMÜ VERİ EKRANI.xlsm"] = (Path.Combine(excelPath, "EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm"), "KAYIT"),
                ["EBATLAMA BÖLÜMÜ VERİ EKRANI.xlsm"] = (Path.Combine(excelPath, "EBATLAMA BÖLÜMÜ VERİ EKRANI 2026.xlsm"), "KAYIT"),
                ["HATALI PARÇA VERİ GİRİŞİ.xlsm"] = (Path.Combine(excelPath, "HATALI PARÇA VERİ GİRİŞİ.xlsm"), "VERİ KAYIT"),
                ["Günlük Personel Sayısı.xlsm"] = (Path.Combine(excelPath, "Günlük Personel Sayısı.xlsm"), "YOKLAMA TABLOSU"),
                ["Günlük Personel Sayısı (1).xlsm"] = (Path.Combine(excelPath, "Günlük Personel Sayısı.xlsm"), "YOKLAMA TABLOSU"),
                ["Günlük Personel Sayısı (1).xlsm"] = (Path.Combine(excelPath, "Günlük Personel Sayısı.xlsm"), "YOKLAMA TABLOSU"),
                ["Günlük Çalışma Verileri.xlsx"] = (Path.Combine(excelPath, "Günlük Çalışma Verileri.xlsx"), "Veriler"),
                ["Günlük Çalışma Verileri 1.xlsx"] = (Path.Combine(excelPath, "Günlük Çalışma Verileri.xlsx"), "Veriler"),
                ["Günlük Çalışma Verileri 1.xlsx"] = (Path.Combine(excelPath, "Günlük Çalışma Verileri.xlsx"), "Veriler")
            };

            foreach (var file in model.Dosyalar ?? new List<IFormFile>())
            {
                if (file == null || file.Length == 0)
                {
                    continue;
                }

                var fileName = Path.GetFileName(file.FileName);
                if (!fileMap.TryGetValue(fileName, out var info))
                {
                    var normalizedFileName = fileName.Normalize(NormalizationForm.FormKC);
                    var matchedKey = fileMap.Keys.FirstOrDefault(key =>
                        string.Equals(
                            key.Normalize(NormalizationForm.FormKC),
                            normalizedFileName,
                            StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(matchedKey))
                    {
                        info = fileMap[matchedKey];
                    }
                    else
                    {
                        results.Add(new VeriYukleResult
                        {
                            DosyaAdi = fileName,
                            SayfaAdi = "-",
                            SatirSayisi = null,
                            Mesaj = "Bu dosya adı tanımlı değil."
                        });
                        continue;
                    }
                }

                HandleUpload(file, info.TargetPath, info.SheetName, results);
            }

            try
            {
                await _dashboardIngestionService.RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Upload sonrası dashboard ingest yenilemesi başarısız oldu.");
            }

            model.Results = results;
            return View(model);
        }

        private void HandleUpload(IFormFile? file, string targetPath, string sheetName, List<VeriYukleResult> results)
        {
            if (file == null || file.Length == 0)
            {
                return;
            }

            try
            {
                using (var stream = new FileStream(targetPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                int? rowCount = null;
                string? message = null;

                try
                {
                    using (var package = new ExcelPackage(new FileInfo(targetPath)))
                    {
                        var worksheet = package.Workbook.Worksheets[sheetName];
                        if (worksheet == null)
                        {
                            message = $"'{sheetName}' sayfası bulunamadı.";
                        }
                        else
                        {
                            rowCount = worksheet.Dimension?.Rows > 1 ? worksheet.Dimension.Rows - 1 : 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Yüklenen Excel doğrulanamadı. Dosya: {FileName}", Path.GetFileName(targetPath));
                    message = "Dosya yüklendi ancak içerik doğrulaması başarısız oldu.";
                }

                results.Add(new VeriYukleResult
                {
                    DosyaAdi = Path.GetFileName(targetPath),
                    SayfaAdi = sheetName,
                    SatirSayisi = rowCount,
                    Mesaj = message ?? "Yüklendi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yükleme hatası. Dosya: {FileName}", Path.GetFileName(targetPath));
                results.Add(new VeriYukleResult
                {
                    DosyaAdi = Path.GetFileName(targetPath),
                    SayfaAdi = sheetName,
                    SatirSayisi = null,
                    Mesaj = "Dosya yüklenemedi."
                });
            }
        }

        private string? ResolveGunlukCalismaDosyaYolu()
        {
            var excelPath = Path.Combine(_hostingEnvironment.WebRootPath, "EXCELS");
            foreach (var aday in GunlukCalismaDosyaAdaylari)
            {
                var dosyaYolu = Path.Combine(excelPath, aday);
                if (System.IO.File.Exists(dosyaYolu))
                {
                    return dosyaYolu;
                }
            }

            return null;
        }

        private static GunlukVeriRaporModel? HazirlaGunlukVeriRaporModeli(
            string dosyaYolu,
            DateTime? raporTarihi,
            DateTime? baslangicTarihi,
            DateTime? bitisTarihi,
            int? ay,
            int? yil)
        {
            using var package = new ExcelPackage(new FileInfo(dosyaYolu));
            var verilerSayfasi = FindWorksheet(package.Workbook, "Veriler");
            if (verilerSayfasi?.Dimension == null)
            {
                return null;
            }

            var hamBolumSatirlari = OkuVerilerSayfasi(verilerSayfasi);
            if (hamBolumSatirlari.Count == 0)
            {
                return null;
            }

            var tumTarihler = hamBolumSatirlari
                .Select(x => x.Tarih.Date)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (tumTarihler.Count == 0)
            {
                return null;
            }

            DateTime donemBaslangic;
            DateTime donemBitis;
            string donemEtiketi;

            if (baslangicTarihi.HasValue || bitisTarihi.HasValue)
            {
                var start = (baslangicTarihi ?? bitisTarihi)!.Value.Date;
                var end = (bitisTarihi ?? baslangicTarihi)!.Value.Date;
                if (start <= end)
                {
                    donemBaslangic = start;
                    donemBitis = end;
                }
                else
                {
                    donemBaslangic = end;
                    donemBitis = start;
                }

                donemEtiketi = donemBaslangic == donemBitis
                    ? $"Gun: {donemBaslangic:dd.MM.yyyy}"
                    : $"Tarih Araligi: {donemBaslangic:dd.MM.yyyy} - {donemBitis:dd.MM.yyyy}";
            }
            else if (ay.HasValue)
            {
                var month = Math.Clamp(ay.Value, 1, 12);
                int resolvedYear;
                if (yil.HasValue)
                {
                    resolvedYear = yil.Value;
                }
                else
                {
                    resolvedYear = tumTarihler
                        .Where(x => x.Month == month)
                        .Select(x => x.Year)
                        .DefaultIfEmpty(tumTarihler.Max(x => x.Year))
                        .Max();
                }

                donemBaslangic = new DateTime(resolvedYear, month, 1);
                donemBitis = donemBaslangic.AddMonths(1).AddDays(-1);
                donemEtiketi = $"Ay: {month:00}.{resolvedYear}";
            }
            else
            {
                var secilenGun = raporTarihi?.Date ?? tumTarihler.Max();
                donemBaslangic = secilenGun;
                donemBitis = secilenGun;
                donemEtiketi = $"Gun: {secilenGun:dd.MM.yyyy}";
            }

            var filtreliBolumSatirlari = hamBolumSatirlari
                .Where(x => x.Tarih >= donemBaslangic && x.Tarih <= donemBitis)
                .ToList();

            if (filtreliBolumSatirlari.Count == 0)
            {
                return null;
            }

            var raporModeli = new GunlukVeriRaporModel
            {
                RaporTarihi = donemBitis,
                DonemBaslangic = donemBaslangic,
                DonemBitis = donemBitis,
                DonemEtiketi = donemEtiketi
            };

            foreach (var bolumBilgisi in GunlukRaporBolumSirasi)
            {
                var bolumSatirlari = filtreliBolumSatirlari
                    .Where(x => x.BolumAnahtari == bolumBilgisi.BolumAnahtari)
                    .ToList();

                if (bolumSatirlari.Count == 0)
                {
                    raporModeli.Bolumler.Add(new GunlukVeriBolumSatiri
                    {
                        BolumAdi = bolumBilgisi.BolumGosterimAdi
                    });
                    continue;
                }

                int toplamUretimeVerilen = bolumSatirlari.Sum(x => x.UretimeVerilenParca);
                int toplamUretilen = bolumSatirlari.Sum(x => x.UretilenParca);
                int toplamKalan = bolumSatirlari.Sum(x => x.KalanParca);
                int toplamGunlukIsci = bolumSatirlari.Sum(x => x.GunlukIsciSayisi);
                int ortalamaGunlukIsci = bolumSatirlari.Count > 0
                    ? (int)Math.Round(bolumSatirlari.Average(x => x.GunlukIsciSayisi), MidpointRounding.AwayFromZero)
                    : 0;

                double planUyumOrani = toplamUretimeVerilen > 0
                    ? (double)toplamUretilen / toplamUretimeVerilen * 100d
                    : bolumSatirlari.Average(x => x.PlanUyumOrani);

                double isciBasinaParcaSayisi = toplamGunlukIsci > 0
                    ? (double)toplamUretilen / toplamGunlukIsci
                    : bolumSatirlari.Average(x => x.IsciBasinaParcaSayisi);

                int? toplamHatali = AggregateNullableSum(bolumSatirlari.Select(x => x.HataliUrun));

                int? ekDeger = bolumBilgisi.BolumAnahtari switch
                {
                    "KESIM" => AggregateNullableMax(bolumSatirlari.Select(x => x.AhsapRenkCesitliligi)),
                    "PVC" => AggregateNullableMax(bolumSatirlari.Select(x => x.PvcRenkCesitliligi)),
                    "BOYA" => AggregateNullableMax(bolumSatirlari.Select(x => x.BoyaRenkCesitliligi)),
                    _ => null
                };

                raporModeli.Bolumler.Add(new GunlukVeriBolumSatiri
                {
                    BolumAdi = bolumBilgisi.BolumGosterimAdi,
                    UretimeVerilenParca = toplamUretimeVerilen,
                    UretilenParca = toplamUretilen,
                    KalanParca = toplamKalan,
                    PlanUyumOrani = planUyumOrani,
                    GunlukIsciSayisi = ortalamaGunlukIsci,
                    IsciBasinaParcaSayisi = isciBasinaParcaSayisi,
                    HataliUrun = toplamHatali,
                    EkLabel = bolumBilgisi.EkLabel,
                    EkDeger = ekDeger
                });
            }

            raporModeli.ToplamHataliUrunSayisi = raporModeli.Bolumler.Sum(x => x.HataliUrun ?? 0);
            raporModeli.CncUrunCesitliligi = AggregateNullableMax(
                filtreliBolumSatirlari.Where(x => x.BolumAnahtari == "CNC").Select(x => x.UrunCesitliligi)) ?? 0;
            raporModeli.KesimUrunCesitliligi = AggregateNullableMax(
                filtreliBolumSatirlari.Where(x => x.BolumAnahtari == "KESIM").Select(x => x.UrunCesitliligi)) ?? 0;

            var gecikmeSayfasi = FindWorksheet(package.Workbook, "Gecikme Nedeni");
            if (gecikmeSayfasi?.Dimension != null)
            {
                var hamGecikmeler = OkuGecikmeSayfasi(gecikmeSayfasi)
                    .Where(x => x.Tarih >= donemBaslangic && x.Tarih <= donemBitis)
                    .ToList();

                raporModeli.GecikmeAdetSayisi = hamGecikmeler.Sum(x => x.Adet);
                if (raporModeli.GecikmeAdetSayisi > 0)
                {
                    var toplam = raporModeli.GecikmeAdetSayisi;
                    var hammaddeToplam = hamGecikmeler.Where(x => IsHammaddeKaynakli(x.Neden)).Sum(x => x.Adet);
                    var isBeklemesiToplam = hamGecikmeler.Where(x => IsIsBeklemesiKaynakli(x.Neden)).Sum(x => x.Adet);

                    raporModeli.HammaddeKaynakliGecikmeOrani = (double)hammaddeToplam / toplam * 100d;
                    raporModeli.IsBeklemesiKaynakliGecikmeOrani = (double)isBeklemesiToplam / toplam * 100d;
                }

                var gecikmeGruplari = hamGecikmeler
                    .GroupBy(x => x.Neden)
                    .Select(g => new
                    {
                        Neden = g.Key,
                        Adet = g.Sum(x => x.Adet)
                    })
                    .OrderByDescending(x => x.Adet)
                    .Take(5)
                    .ToList();

                foreach (var gecikme in gecikmeGruplari)
                {
                    var oran = raporModeli.GecikmeAdetSayisi > 0
                        ? (double)gecikme.Adet / raporModeli.GecikmeAdetSayisi * 100d
                        : 0d;

                    raporModeli.GecikmeNedenleri.Add(new GunlukVeriGecikmeSatiri
                    {
                        Neden = gecikme.Neden,
                        Adet = gecikme.Adet,
                        Oran = oran
                    });
                }
            }

            return raporModeli;
        }

        private static ExcelWorksheet? FindWorksheet(ExcelWorkbook workbook, string hedefAd)
        {
            return workbook.Worksheets.FirstOrDefault(sheet =>
                string.Equals(
                    sheet.Name.Normalize(NormalizationForm.FormKC),
                    hedefAd.Normalize(NormalizationForm.FormKC),
                    StringComparison.OrdinalIgnoreCase));
        }

        private static List<GunlukVeriBolumHamSatiri> OkuVerilerSayfasi(ExcelWorksheet worksheet)
        {
            var satirlar = new List<GunlukVeriBolumHamSatiri>();
            if (worksheet.Dimension == null)
            {
                return satirlar;
            }

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var bolumHam = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(bolumHam))
                {
                    continue;
                }

                var bolumAnahtari = NormalizeBolumAnahtari(bolumHam);
                if (string.IsNullOrWhiteSpace(bolumAnahtari))
                {
                    continue;
                }

                var tarih = DashboardParsingHelper.ParseDateCell(worksheet.Cells[row, 9].Value, worksheet.Cells[row, 9].Text);
                if (tarih == DateTime.MinValue)
                {
                    continue;
                }

                satirlar.Add(new GunlukVeriBolumHamSatiri
                {
                    BolumAnahtari = bolumAnahtari,
                    Tarih = tarih.Date,
                    UretimeVerilenParca = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 3].Value),
                    UretilenParca = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 4].Value),
                    KalanParca = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 5].Value),
                    PlanUyumOrani = ParsePlanUyumOrani(worksheet.Cells[row, 6].Value, worksheet.Cells[row, 6].Text),
                    GunlukIsciSayisi = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 7].Value),
                    IsciBasinaParcaSayisi = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 8].Value),
                    HataliUrun = ParseNullableInt(worksheet.Cells[row, 10]),
                    BoyaRenkCesitliligi = ParseNullableInt(worksheet.Cells[row, 11]),
                    AhsapRenkCesitliligi = ParseNullableInt(worksheet.Cells[row, 12]),
                    PvcRenkCesitliligi = ParseNullableInt(worksheet.Cells[row, 13]),
                    UrunCesitliligi = ParseNullableInt(worksheet.Cells[row, 14])
                });
            }

            return satirlar;
        }

        private static List<GunlukVeriGecikmeHamSatiri> OkuGecikmeSayfasi(ExcelWorksheet worksheet)
        {
            var satirlar = new List<GunlukVeriGecikmeHamSatiri>();
            if (worksheet.Dimension == null)
            {
                return satirlar;
            }

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var neden = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(neden))
                {
                    continue;
                }

                var tarih = DashboardParsingHelper.ParseDateCell(worksheet.Cells[row, 1].Value, worksheet.Cells[row, 1].Text);
                if (tarih == DateTime.MinValue)
                {
                    continue;
                }

                var adet = DashboardParsingHelper.ParseUretimAdedi(worksheet.Cells[row, 3].Value);
                if (adet <= 0)
                {
                    continue;
                }

                var oranHam = DashboardParsingHelper.ParseDoubleCell(worksheet.Cells[row, 4].Value);
                var oran = oranHam <= 1.0001 ? oranHam * 100d : oranHam;

                satirlar.Add(new GunlukVeriGecikmeHamSatiri
                {
                    Tarih = tarih.Date,
                    Neden = DashboardParsingHelper.NormalizeLabel(neden),
                    Adet = adet,
                    Oran = oran
                });
            }

            return satirlar;
        }

        private static string NormalizeBolumAnahtari(string bolumAdi)
        {
            var normalized = DashboardParsingHelper.NormalizeHeaderForMatch(bolumAdi);
            return normalized switch
            {
                "kesim" => "KESIM",
                "pvc" => "PVC",
                "cnc" => "CNC",
                "keson" => "KESON",
                "metal" => "METAL",
                "boya" => "BOYA",
                "montaj" => "MONTAJ",
                "saclazer" => "SAC LAZER",
                _ => string.Empty
            };
        }

        private static double ParsePlanUyumOrani(object? value, string? text)
        {
            var oran = DashboardParsingHelper.ParsePercentCell(value);
            if (oran <= 0 && !string.IsNullOrWhiteSpace(text))
            {
                oran = DashboardParsingHelper.ParsePercentCell(text);
            }

            return oran <= 1.0001 ? oran * 100d : oran;
        }

        private static int? ParseNullableInt(ExcelRange cell)
        {
            if (cell.Value == null && string.IsNullOrWhiteSpace(cell.Text))
            {
                return null;
            }

            return DashboardParsingHelper.ParseUretimAdedi(cell.Value ?? cell.Text);
        }

        private static int? AggregateNullableSum(IEnumerable<int?> values)
        {
            var filtered = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (filtered.Count == 0)
            {
                return null;
            }

            return filtered.Sum();
        }

        private static int? AggregateNullableMax(IEnumerable<int?> values)
        {
            var filtered = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (filtered.Count == 0)
            {
                return null;
            }

            return filtered.Max();
        }

        private static bool IsHammaddeKaynakli(string neden)
        {
            var normalized = DashboardParsingHelper.NormalizeHeaderForMatch(neden);
            return normalized.Contains("hammaddeeksik", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIsBeklemesiKaynakli(string neden)
        {
            var normalized = DashboardParsingHelper.NormalizeHeaderForMatch(neden);
            return normalized.Contains("isgecikmesi", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("isgelmedi", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("kurumdangelmedi", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("urunlerhazir", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("tedarikcikaynakliisgecikmesi", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildGunlukVeriPdfDosyaAdi(GunlukVeriRaporModel model)
        {
            if (model.DonemBaslangic != DateTime.MinValue
                && model.DonemBitis != DateTime.MinValue
                && model.DonemBaslangic.Date != model.DonemBitis.Date)
            {
                return $"gunluk-veriler-{model.DonemBaslangic:yyyy-MM-dd}_{model.DonemBitis:yyyy-MM-dd}.pdf";
            }

            var day = model.RaporTarihi == DateTime.MinValue ? DateTime.Today : model.RaporTarihi;
            return $"gunluk-veriler-{day:dd.MM.yyyy}.pdf";
        }

        private static byte[] BuildGunlukVeriStyledPdf(GunlukVeriRaporModel model)
        {
            const double pageWidth = 1348.08;
            const double pageHeight = 1056;

            var content = BuildGunlukVeriStyledContent(model, pageWidth, pageHeight);
            var contentBytes = Encoding.ASCII.GetBytes(content);

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
            var offsets = new List<long> { 0 };

            writer.WriteLine("%PDF-1.4");
            writer.Flush();

            void StartObject(int objectId)
            {
                writer.Flush();
                offsets.Add(ms.Position);
                writer.WriteLine($"{objectId} 0 obj");
            }

            StartObject(1);
            writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
            writer.WriteLine("endobj");

            StartObject(2);
            writer.WriteLine("<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
            writer.WriteLine("endobj");

            StartObject(3);
            writer.WriteLine($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth.ToString(CultureInfo.InvariantCulture)} {pageHeight.ToString(CultureInfo.InvariantCulture)}] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>");
            writer.WriteLine("endobj");

            StartObject(4);
            writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            writer.WriteLine("endobj");

            StartObject(5);
            writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            writer.WriteLine("endobj");

            StartObject(6);
            writer.WriteLine($"<< /Length {contentBytes.Length} >>");
            writer.WriteLine("stream");
            writer.Flush();
            ms.Write(contentBytes, 0, contentBytes.Length);
            writer.WriteLine();
            writer.WriteLine("endstream");
            writer.WriteLine("endobj");
            writer.Flush();

            var xrefStart = ms.Position;
            writer.WriteLine("xref");
            writer.WriteLine("0 7");
            writer.WriteLine("0000000000 65535 f ");
            for (int i = 1; i <= 6; i++)
            {
                writer.WriteLine($"{offsets[i]:D10} 00000 n ");
            }

            writer.WriteLine("trailer");
            writer.WriteLine("<< /Size 7 /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefStart.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("%%EOF");
            writer.Flush();

            return ms.ToArray();
        }

        private static string BuildGunlukVeriStyledContent(GunlukVeriRaporModel model, double pageWidth, double pageHeight)
        {
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            static string FormatInt(int value, CultureInfo culture) => value.ToString("N0", culture);
            static string FormatNullableInt(int? value, CultureInfo culture) => value.HasValue ? value.Value.ToString("N0", culture) : "(Bos)";
            static string FormatPercent(double value, CultureInfo culture) => "%" + value.ToString("N2", culture);
            static string FormatDecimal(double value, CultureInfo culture) => value.ToString("N2", culture);

            double ToPdfY(double top, double height) => pageHeight - top - height;

            void FillRect(double x, double top, double width, double height, double r, double g, double b)
            {
                sb.AppendFormat(ci, "{0:0.###} {1:0.###} {2:0.###} rg\n", r, g, b);
                sb.AppendFormat(ci, "{0:0.##} {1:0.##} {2:0.##} {3:0.##} re f\n", x, ToPdfY(top, height), width, height);
            }

            void StrokeRect(double x, double top, double width, double height, double lineWidth, double r, double g, double b)
            {
                sb.AppendFormat(ci, "{0:0.###} {1:0.###} {2:0.###} RG\n", r, g, b);
                sb.AppendFormat(ci, "{0:0.##} w\n", lineWidth);
                sb.AppendFormat(ci, "{0:0.##} {1:0.##} {2:0.##} {3:0.##} re S\n", x, ToPdfY(top, height), width, height);
            }

            double EstimateTextWidth(string text, double fontSize)
            {
                var normalized = ToPdfAscii(text);
                return normalized.Length * fontSize * 0.52;
            }

            void DrawText(string text, double x, double top, string font, double size, double r, double g, double b)
            {
                var normalized = ToPdfAscii(text);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    return;
                }

                sb.AppendFormat(ci, "{0:0.###} {1:0.###} {2:0.###} rg\n", r, g, b);
                sb.Append("BT\n");
                sb.AppendFormat(ci, "/{0} {1:0.##} Tf\n", font, size);
                sb.AppendFormat(ci, "{0:0.##} {1:0.##} Td\n", x, pageHeight - top - size);
                sb.AppendFormat("({0}) Tj\n", EscapePdfString(normalized));
                sb.Append("ET\n");
            }

            void DrawCenteredText(string text, double boxX, double boxTop, double boxWidth, double boxHeight, string font, double size, double r, double g, double b, double topOffset = 0)
            {
                var width = EstimateTextWidth(text, size);
                var textX = boxX + Math.Max(4, (boxWidth - width) / 2d);
                var textTop = boxTop + ((boxHeight - size) / 2d) + topOffset;
                DrawText(text, textX, textTop, font, size, r, g, b);
            }

            var leftX = 18d;
            var leftWidth = 986d;
            var rightX = 1020d;
            var rightWidth = pageWidth - rightX - 18d;
            var topMargin = 30d;
            var blockGap = 6d;
            var sectionHeight = 122d;
            var headerHeight = 36d;
            var metricsTopOffset = headerHeight;
            var metricsHeight = sectionHeight - headerHeight;

            FillRect(0, 0, pageWidth, pageHeight, 1, 1, 1);
            DrawText("Marwood Gunluk Veriler Raporu", leftX, 6, "F2", 14, 0.1, 0.1, 0.1);
            DrawText(model.DonemEtiketi, leftX, 20, "F1", 10.5, 0.25, 0.25, 0.25);

            for (int i = 0; i < GunlukRaporBolumSirasi.Length; i++)
            {
                var bolum = i < model.Bolumler.Count
                    ? model.Bolumler[i]
                    : new GunlukVeriBolumSatiri { BolumAdi = GunlukRaporBolumSirasi[i].BolumGosterimAdi };
                var sectionTop = topMargin + i * (sectionHeight + blockGap);

                FillRect(leftX, sectionTop, leftWidth, headerHeight, 0.12, 0.76, 0.67);
                StrokeRect(leftX, sectionTop, leftWidth, headerHeight, 1, 0.2, 0.2, 0.2);
                DrawCenteredText(bolum.BolumAdi, leftX, sectionTop, leftWidth, headerHeight, "F2", 18, 0.1, 0.1, 0.12);

                var metricLabels = new[]
                {
                    "Uretime Verilen Parca Sayisi",
                    "Uretilen Parca Sayisi",
                    "Kalan Parca Sayisi",
                    "Plana Uyum Orani",
                    "Gunluk Isci Sayisi",
                    "Isci Bas. Dus. Par.Say.",
                    "Hatali Urun Sayisi",
                    string.IsNullOrWhiteSpace(bolum.EkLabel) ? "-" : bolum.EkLabel!
                };

                var metricValues = new[]
                {
                    FormatInt(bolum.UretimeVerilenParca, tr),
                    FormatInt(bolum.UretilenParca, tr),
                    FormatInt(bolum.KalanParca, tr),
                    FormatPercent(bolum.PlanUyumOrani, tr),
                    FormatInt(bolum.GunlukIsciSayisi, tr),
                    FormatDecimal(bolum.IsciBasinaParcaSayisi, tr),
                    FormatNullableInt(bolum.HataliUrun, tr),
                    string.IsNullOrWhiteSpace(bolum.EkLabel) ? "-" : FormatNullableInt(bolum.EkDeger, tr)
                };

                var cardCount = metricLabels.Length;
                var cellGap = 2d;
                var cellWidth = (leftWidth - ((cardCount - 1) * cellGap)) / cardCount;
                var cardsTop = sectionTop + metricsTopOffset;

                for (int metricIndex = 0; metricIndex < cardCount; metricIndex++)
                {
                    var cellX = leftX + metricIndex * (cellWidth + cellGap);
                    FillRect(cellX, cardsTop, cellWidth, metricsHeight, 0.93, 0.93, 0.93);
                    StrokeRect(cellX, cardsTop, cellWidth, metricsHeight, 0.9, 0.25, 0.25, 0.25);

                    DrawCenteredText(metricValues[metricIndex], cellX, cardsTop + 10, cellWidth, 30, "F2", 18, 0.15, 0.15, 0.16);

                    var wrappedLabels = WrapLine(metricLabels[metricIndex], 20).Take(2).ToList();
                    for (int labelLine = 0; labelLine < wrappedLabels.Count; labelLine++)
                    {
                        DrawCenteredText(
                            wrappedLabels[labelLine],
                            cellX,
                            cardsTop + 52 + (labelLine * 9),
                            cellWidth,
                            10,
                            "F1",
                            8.5,
                            0.32,
                            0.32,
                            0.32);
                    }
                }
            }

            var rightTop = topMargin;
            var topRedHeight = (sectionHeight * 3) + (blockGap * 2);
            FillRect(rightX, rightTop, rightWidth, topRedHeight, 0.66, 0.2, 0.25);
            StrokeRect(rightX, rightTop, rightWidth, topRedHeight, 1, 0.25, 0.15, 0.15);
            DrawCenteredText(FormatInt(model.ToplamHataliUrunSayisi, tr), rightX, rightTop + (topRedHeight * 0.42), rightWidth, 40, "F2", 44, 0, 0, 0);
            DrawCenteredText("Toplam Hatali Urun Sayisi", rightX, rightTop + (topRedHeight * 0.6), rightWidth, 40, "F2", 16, 0, 0, 0);

            var midTop = rightTop + topRedHeight + blockGap;
            var midHeight = (sectionHeight * 2) + blockGap;
            var midCellWidth = (rightWidth - blockGap) / 2d;

            FillRect(rightX, midTop, midCellWidth, midHeight, 0.89, 0.84, 0.57);
            StrokeRect(rightX, midTop, midCellWidth, midHeight, 1, 0.4, 0.35, 0.2);
            DrawCenteredText(FormatInt(model.CncUrunCesitliligi, tr), rightX, midTop + (midHeight * 0.42), midCellWidth, 34, "F2", 42, 0.13, 0.13, 0.13);
            DrawCenteredText("CNC Urun Cesitliligi", rightX, midTop + (midHeight * 0.62), midCellWidth, 30, "F2", 12.5, 0.35, 0.35, 0.35);

            var midRightX = rightX + midCellWidth + blockGap;
            FillRect(midRightX, midTop, midCellWidth, midHeight, 0.89, 0.84, 0.57);
            StrokeRect(midRightX, midTop, midCellWidth, midHeight, 1, 0.4, 0.35, 0.2);
            DrawCenteredText(FormatInt(model.KesimUrunCesitliligi, tr), midRightX, midTop + (midHeight * 0.42), midCellWidth, 34, "F2", 42, 0.13, 0.13, 0.13);
            DrawCenteredText("Kesim Urun Cesitliligi", midRightX, midTop + (midHeight * 0.62), midCellWidth, 30, "F2", 12.5, 0.35, 0.35, 0.35);

            var bottomTop = midTop + midHeight + blockGap;
            var bottomHeight = (sectionHeight * 3) + (blockGap * 2);
            var reasonHeight = 225d;

            FillRect(rightX, bottomTop, rightWidth, reasonHeight, 0.91, 0.47, 0.24);
            StrokeRect(rightX, bottomTop, rightWidth, reasonHeight, 1, 0.45, 0.24, 0.12);
            DrawText("Montajda Geride Kalmaya Sebep Olan Nedenler", rightX + 10, bottomTop + 12, "F2", 12, 0.08, 0.08, 0.08);

            var reasonTextTop = bottomTop + 32;
            var reasonLines = new List<string>();
            if (model.GecikmeNedenleri.Count == 0)
            {
                reasonLines.Add("- Secili tarihte gecikme kaydi bulunamadi.");
            }
            else
            {
                foreach (var reason in model.GecikmeNedenleri.Take(4))
                {
                    var line = $"- {reason.Neden} ({FormatInt(reason.Adet, tr)})";
                    reasonLines.AddRange(WrapLine(line, 63).Take(2));
                }
            }

            for (int i = 0; i < reasonLines.Count; i++)
            {
                DrawText(reasonLines[i], rightX + 12, reasonTextTop + i * 12, "F1", 10.5, 0.1, 0.1, 0.1);
            }

            var bottomStatsTop = bottomTop + reasonHeight + blockGap;
            var bottomStatsHeight = bottomHeight - reasonHeight - blockGap;
            var leftStatsWidth = rightWidth * 0.66;
            var rightStatsX = rightX + leftStatsWidth + blockGap;
            var rightStatsWidth = rightWidth - leftStatsWidth - blockGap;

            FillRect(rightX, bottomStatsTop, leftStatsWidth, bottomStatsHeight, 0.91, 0.47, 0.24);
            StrokeRect(rightX, bottomStatsTop, leftStatsWidth, bottomStatsHeight, 1, 0.45, 0.24, 0.12);
            DrawCenteredText(FormatInt(model.GecikmeAdetSayisi, tr), rightX, bottomStatsTop + bottomStatsHeight * 0.38, leftStatsWidth, 44, "F2", 58, 0.1, 0.1, 0.1);
            DrawCenteredText("Gecikme Adet Sayisi", rightX, bottomStatsTop + bottomStatsHeight * 0.65, leftStatsWidth, 24, "F2", 16, 0.25, 0.25, 0.25);

            var stackedHeight = (bottomStatsHeight - blockGap) / 2d;
            FillRect(rightStatsX, bottomStatsTop, rightStatsWidth, stackedHeight, 0.91, 0.47, 0.24);
            StrokeRect(rightStatsX, bottomStatsTop, rightStatsWidth, stackedHeight, 1, 0.45, 0.24, 0.12);
            DrawCenteredText(FormatPercent(model.HammaddeKaynakliGecikmeOrani, tr), rightStatsX, bottomStatsTop + stackedHeight * 0.34, rightStatsWidth, 30, "F2", 28, 0.1, 0.1, 0.1);
            DrawCenteredText("Hammadde Kaynakli Gecikme Orani", rightStatsX, bottomStatsTop + stackedHeight * 0.62, rightStatsWidth, 20, "F2", 11.5, 0.25, 0.25, 0.25);

            var lowerStackTop = bottomStatsTop + stackedHeight + blockGap;
            FillRect(rightStatsX, lowerStackTop, rightStatsWidth, stackedHeight, 0.91, 0.47, 0.24);
            StrokeRect(rightStatsX, lowerStackTop, rightStatsWidth, stackedHeight, 1, 0.45, 0.24, 0.12);
            DrawCenteredText(FormatPercent(model.IsBeklemesiKaynakliGecikmeOrani, tr), rightStatsX, lowerStackTop + stackedHeight * 0.34, rightStatsWidth, 30, "F2", 28, 0.1, 0.1, 0.1);
            DrawCenteredText("Is Beklemesi Kaynakli Gecikme Orani", rightStatsX, lowerStackTop + stackedHeight * 0.62, rightStatsWidth, 20, "F2", 11.5, 0.25, 0.25, 0.25);

            return sb.ToString();
        }

        private static IReadOnlyList<string> BuildGunlukVeriPdfSatirlari(GunlukVeriRaporModel model)
        {
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            var satirlar = new List<string>
            {
                "MARWOOD GUNLUK VERILER RAPORU",
                $"Rapor Tarihi: {model.RaporTarihi:dd.MM.yyyy}",
                new string('-', 128),
                "BOLUM      URETIME VERILEN   URETILEN    KALAN   PLAN UYUM %   GUNLUK ISCI   ISCI BASINA PARCA   HATALI URUN   EK METRIK",
                new string('-', 128)
            };

            foreach (var bolum in model.Bolumler)
            {
                var ekMetin = bolum.EkLabel == null
                    ? "-"
                    : $"{bolum.EkLabel}: {FormatOptionalInt(bolum.EkDeger, tr)}";

                satirlar.Add(string.Format(
                    tr,
                    "{0,-10} {1,16} {2,10} {3,8} {4,13} {5,12} {6,19} {7,12}   {8}",
                    bolum.BolumAdi,
                    bolum.UretimeVerilenParca.ToString("N0", tr),
                    bolum.UretilenParca.ToString("N0", tr),
                    bolum.KalanParca.ToString("N0", tr),
                    "%" + bolum.PlanUyumOrani.ToString("N2", tr),
                    bolum.GunlukIsciSayisi.ToString("N0", tr),
                    bolum.IsciBasinaParcaSayisi.ToString("N2", tr),
                    FormatOptionalInt(bolum.HataliUrun, tr),
                    ekMetin));
            }

            satirlar.Add(new string('-', 128));
            satirlar.Add($"Toplam Hatali Urun Sayisi: {model.ToplamHataliUrunSayisi.ToString("N0", tr)}");
            satirlar.Add($"CNC Urun Cesitliligi: {model.CncUrunCesitliligi.ToString("N0", tr)}");
            satirlar.Add($"Kesim Urun Cesitliligi: {model.KesimUrunCesitliligi.ToString("N0", tr)}");
            satirlar.Add($"Gecikme Adet Sayisi: {model.GecikmeAdetSayisi.ToString("N0", tr)}");
            satirlar.Add($"Hammadde Kaynakli Gecikme Orani: %{model.HammaddeKaynakliGecikmeOrani.ToString("N2", tr)}");
            satirlar.Add($"Is Beklemesi Kaynakli Gecikme Orani: %{model.IsBeklemesiKaynakliGecikmeOrani.ToString("N2", tr)}");
            satirlar.Add(new string('-', 128));
            satirlar.Add("Montajda Geride Kalmaya Sebep Olan Nedenler:");

            if (model.GecikmeNedenleri.Count == 0)
            {
                satirlar.Add("- Secili tarihte gecikme kaydi bulunamadi.");
            }
            else
            {
                foreach (var gecikme in model.GecikmeNedenleri)
                {
                    satirlar.Add($"- {gecikme.Neden} (Adet: {gecikme.Adet.ToString("N0", tr)}, Oran: %{gecikme.Oran.ToString("N2", tr)})");
                }
            }

            return satirlar;
        }

        private static string FormatOptionalInt(int? value, CultureInfo culture)
        {
            return value.HasValue ? value.Value.ToString("N0", culture) : "-";
        }

        private static byte[] BuildSimplePdf(IReadOnlyList<string> satirlar)
        {
            const int pageWidth = 842;
            const int pageHeight = 595;
            const int margin = 28;
            const int lineHeight = 13;
            const int wrapLength = 130;

            var tumSatirlar = new List<string>();
            foreach (var satir in satirlar)
            {
                var ascii = ToPdfAscii(satir);
                foreach (var wrapped in WrapLine(ascii, wrapLength))
                {
                    tumSatirlar.Add(wrapped);
                }
            }

            int linesPerPage = Math.Max(1, (pageHeight - (2 * margin)) / lineHeight);
            var sayfalar = new List<List<string>>();
            for (int i = 0; i < tumSatirlar.Count; i += linesPerPage)
            {
                sayfalar.Add(tumSatirlar.Skip(i).Take(linesPerPage).ToList());
            }
            if (sayfalar.Count == 0)
            {
                sayfalar.Add(new List<string> { "Rapor verisi bulunamadi." });
            }

            int pageCount = sayfalar.Count;
            int totalObjects = 3 + (pageCount * 2);

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
            var offsets = new List<long> { 0 };

            writer.WriteLine("%PDF-1.4");
            writer.Flush();

            for (int objId = 1; objId <= totalObjects; objId++)
            {
                writer.Flush();
                offsets.Add(ms.Position);
                writer.WriteLine($"{objId} 0 obj");

                if (objId == 1)
                {
                    writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
                    writer.WriteLine("endobj");
                    continue;
                }

                if (objId == 2)
                {
                    var pageRefs = string.Join(' ', Enumerable.Range(0, pageCount).Select(i => $"{4 + (i * 2)} 0 R"));
                    writer.WriteLine($"<< /Type /Pages /Count {pageCount} /Kids [{pageRefs}] >>");
                    writer.WriteLine("endobj");
                    continue;
                }

                if (objId == 3)
                {
                    writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                    writer.WriteLine("endobj");
                    continue;
                }

                bool isPageObject = objId % 2 == 0;
                int pageIndex = (objId - 4) / 2;

                if (isPageObject)
                {
                    int contentObjectId = objId + 1;
                    writer.WriteLine($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>");
                    writer.WriteLine("endobj");
                }
                else
                {
                    var stream = BuildPdfContentStream(sayfalar[pageIndex], margin, pageHeight - margin, lineHeight);
                    writer.WriteLine($"<< /Length {stream.Length} >>");
                    writer.WriteLine("stream");
                    writer.Flush();
                    ms.Write(stream, 0, stream.Length);
                    writer.WriteLine();
                    writer.WriteLine("endstream");
                    writer.WriteLine("endobj");
                }
            }

            writer.Flush();
            long xrefStart = ms.Position;

            writer.WriteLine($"xref\n0 {totalObjects + 1}");
            writer.WriteLine("0000000000 65535 f ");
            for (int i = 1; i <= totalObjects; i++)
            {
                writer.WriteLine($"{offsets[i]:D10} 00000 n ");
            }

            writer.WriteLine("trailer");
            writer.WriteLine($"<< /Size {totalObjects + 1} /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefStart.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("%%EOF");
            writer.Flush();

            return ms.ToArray();
        }

        private static byte[] BuildPdfContentStream(IReadOnlyList<string> lines, int startX, int startY, int lineHeight)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine($"{lineHeight} TL");
            sb.AppendLine($"{startX} {startY} Td");

            for (int i = 0; i < lines.Count; i++)
            {
                var escaped = EscapePdfString(lines[i]);
                if (i == 0)
                {
                    sb.Append('(').Append(escaped).AppendLine(") Tj");
                }
                else
                {
                    sb.Append("T* (").Append(escaped).AppendLine(") Tj");
                }
            }

            sb.AppendLine("ET");
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static IEnumerable<string> WrapLine(string line, int maxLength)
        {
            if (string.IsNullOrEmpty(line))
            {
                yield return string.Empty;
                yield break;
            }

            var remaining = line;
            while (remaining.Length > maxLength)
            {
                var splitIndex = remaining.LastIndexOf(' ', maxLength);
                if (splitIndex <= 0)
                {
                    splitIndex = maxLength;
                }

                yield return remaining[..splitIndex].TrimEnd();
                remaining = remaining[splitIndex..].TrimStart();
            }

            yield return remaining;
        }

        private static string ToPdfAscii(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                switch (ch)
                {
                    case '\r':
                    case '\n':
                    case '\t':
                        sb.Append(' ');
                        break;
                    case 'ı':
                        sb.Append('i');
                        break;
                    case 'İ':
                        sb.Append('I');
                        break;
                    case 'ş':
                        sb.Append('s');
                        break;
                    case 'Ş':
                        sb.Append('S');
                        break;
                    case 'ğ':
                        sb.Append('g');
                        break;
                    case 'Ğ':
                        sb.Append('G');
                        break;
                    case 'ü':
                        sb.Append('u');
                        break;
                    case 'Ü':
                        sb.Append('U');
                        break;
                    case 'ö':
                        sb.Append('o');
                        break;
                    case 'Ö':
                        sb.Append('O');
                        break;
                    case 'ç':
                        sb.Append('c');
                        break;
                    case 'Ç':
                        sb.Append('C');
                        break;
                    default:
                        sb.Append(ch is >= ' ' and <= '~' ? ch : '?');
                        break;
                }
            }

            return sb.ToString();
        }

        private static string EscapePdfString(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private sealed class GunlukVeriRaporModel
        {
            public DateTime RaporTarihi { get; set; }
            public DateTime DonemBaslangic { get; set; }
            public DateTime DonemBitis { get; set; }
            public string DonemEtiketi { get; set; } = string.Empty;
            public List<GunlukVeriBolumSatiri> Bolumler { get; } = new List<GunlukVeriBolumSatiri>();
            public int ToplamHataliUrunSayisi { get; set; }
            public int CncUrunCesitliligi { get; set; }
            public int KesimUrunCesitliligi { get; set; }
            public int GecikmeAdetSayisi { get; set; }
            public double HammaddeKaynakliGecikmeOrani { get; set; }
            public double IsBeklemesiKaynakliGecikmeOrani { get; set; }
            public List<GunlukVeriGecikmeSatiri> GecikmeNedenleri { get; } = new List<GunlukVeriGecikmeSatiri>();
        }

        private sealed class GunlukVeriBolumSatiri
        {
            public string BolumAdi { get; set; } = string.Empty;
            public int UretimeVerilenParca { get; set; }
            public int UretilenParca { get; set; }
            public int KalanParca { get; set; }
            public double PlanUyumOrani { get; set; }
            public int GunlukIsciSayisi { get; set; }
            public double IsciBasinaParcaSayisi { get; set; }
            public int? HataliUrun { get; set; }
            public string? EkLabel { get; set; }
            public int? EkDeger { get; set; }
        }

        private sealed class GunlukVeriBolumHamSatiri
        {
            public string BolumAnahtari { get; set; } = string.Empty;
            public DateTime Tarih { get; set; }
            public int UretimeVerilenParca { get; set; }
            public int UretilenParca { get; set; }
            public int KalanParca { get; set; }
            public double PlanUyumOrani { get; set; }
            public int GunlukIsciSayisi { get; set; }
            public double IsciBasinaParcaSayisi { get; set; }
            public int? HataliUrun { get; set; }
            public int? BoyaRenkCesitliligi { get; set; }
            public int? AhsapRenkCesitliligi { get; set; }
            public int? PvcRenkCesitliligi { get; set; }
            public int? UrunCesitliligi { get; set; }
        }

        private sealed class GunlukVeriGecikmeHamSatiri
        {
            public DateTime Tarih { get; set; }
            public string Neden { get; set; } = string.Empty;
            public int Adet { get; set; }
            public double Oran { get; set; }
        }

        private sealed class GunlukVeriGecikmeSatiri
        {
            public string Neden { get; set; } = string.Empty;
            public int Adet { get; set; }
            public double Oran { get; set; }
        }
    }
}
