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

namespace ONERI.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FabrikaContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IDashboardIngestionService _dashboardIngestionService;
        private readonly ILogger<AdminController> _logger;

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
                ["Günlük Personel Sayısı (1).xlsm"] = (Path.Combine(excelPath, "Günlük Personel Sayısı.xlsm"), "YOKLAMA TABLOSU")
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
                    results.Add(new VeriYukleResult
                    {
                        DosyaAdi = fileName,
                        SayfaAdi = "-",
                        SatirSayisi = null,
                        Mesaj = "Bu dosya adı tanımlı değil."
                    });
                    continue;
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
    }
}
