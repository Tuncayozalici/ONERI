using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ONERI.Models;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Hosting;

namespace ONERI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult GunlukVeriler()
        {
            return View();
        }

        public IActionResult ProfilLazerVerileri(DateTime? raporTarihi)
        {
            var islenecekTarih = raporTarihi ?? DateTime.Today;

            var viewModel = new GunlukVerilerViewModel
            {
                RaporTarihi = islenecekTarih,
                ProfilIsimleri = new List<string>(),
                ProfilUretimAdetleri = new List<int>(),
                Son7GunTarihleri = new List<string>(),
                GunlukUretimSayilari = new List<int>(),
                UrunIsimleri = new List<string>(),
                UrunHarcananSure = new List<int>()
            };

            string rootPath = _hostingEnvironment.WebRootPath;
            string filePath = Path.Combine(rootPath, "EXCELS", "MARWOOD Profil Lazer Veri Ekranı.xlsm");
            
            if (!System.IO.File.Exists(filePath))
            {
                ViewBag.ErrorMessage = "Excel dosyası bulunamadı: " + filePath;
                return View(viewModel);
            }

            var excelData = new List<SatirModeli>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets["LAZER KAYIT"];
                if (worksheet == null)
                {
                    ViewBag.ErrorMessage = "'LAZER KAYIT' sayfası bulunamadı.";
                    return View(viewModel);
                }

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // 1. satır başlık varsayıldı
                {
                    try
                    {
                        var dateValue = worksheet.Cells[row, 1].Value;
                        var dateString = dateValue?.ToString() ?? string.Empty;
                        
                        excelData.Add(new SatirModeli
                        {
                            Tarih = ParseTurkishDate(dateString),
                            MusteriAdi = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                            ProfilTipi = worksheet.Cells[row, 4].Value?.ToString()?.ToUpper().Trim(),
                            UretimAdedi = Convert.ToInt32(worksheet.Cells[row, 5].Value),
                            CalismaSuresi = Convert.ToInt32(worksheet.Cells[row, 6].Value)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Satır {row} okunurken hata: {ex.Message}");
                        // Hatalı satırları atla veya bir hata mesajı göster
                    }
                }
            }

            // Adım 2.2: Hesaplama (KPI'lar için)
            var gununVerileri = excelData.Where(x => x.Tarih.Date == islenecekTarih.Date).ToList();

            viewModel.GunlukToplamUretim = gununVerileri.Sum(x => x.UretimAdedi);
            viewModel.GunlukToplamSure = gununVerileri.Sum(x => x.CalismaSuresi);

            viewModel.OrtalamaIslemSuresi = viewModel.GunlukToplamUretim > 0 
                ? (double)viewModel.GunlukToplamSure / viewModel.GunlukToplamUretim 
                : 0;

            // Adım 2.3: Gruplama (Pasta Grafik için)
            var pastaGrafikData = gununVerileri
                .GroupBy(x => x.ProfilTipi)
                .Select(g => new { Profil = g.Key, ToplamUretim = g.Sum(x => x.UretimAdedi) })
                .OrderByDescending(x => x.ToplamUretim)
                .ToList();

            viewModel.ProfilIsimleri = pastaGrafikData.Select(x => x.Profil).Where(p => p != null).ToList()!;
            viewModel.ProfilUretimAdetleri = pastaGrafikData.Select(x => x.ToplamUretim).ToList();

            // Adım 2.4: Gruplama (Yeni Pasta Grafik için - Ürün bazlı süre dağılımı)
            var urunBazliSureData = gununVerileri
                .GroupBy(x => x.ProfilTipi)
                .Select(g => new { Urun = g.Key, ToplamSure = g.Sum(x => x.CalismaSuresi) })
                .OrderByDescending(x => x.ToplamSure)
                .ToList();

            var toplamSureTumUrunler = (double)urunBazliSureData.Sum(x => x.ToplamSure);

            viewModel.UrunIsimleri = urunBazliSureData.Select(x => x.Urun).Where(u => u != null).ToList()!;
            viewModel.UrunHarcananSure = urunBazliSureData
                .Select(x => toplamSureTumUrunler > 0 ? (int)Math.Round(x.ToplamSure / toplamSureTumUrunler * 100) : 0)
                .ToList();

            // Adım 2.3: Gruplama (Çizgi Grafik için) - Bu kısım seçilen tarihten bağımsız olarak son 7 günü gösterir
            var son7GunVerileri = excelData
                .Where(x => x.Tarih.Date >= DateTime.Today.AddDays(-6) && x.Tarih.Date <= DateTime.Today)
                .GroupBy(x => x.Tarih.Date)
                .Select(g => new { Tarih = g.Key, ToplamUretim = g.Sum(x => x.UretimAdedi) })
                .OrderBy(x => x.Tarih)
                .ToList();

            viewModel.Son7GunTarihleri = son7GunVerileri.Select(x => x.Tarih.ToString("dd.MM")).ToList();
            
                        viewModel.GunlukUretimSayilari = son7GunVerileri.Select(x => x.ToplamUretim).ToList();
            
                        return View(viewModel);
                    }
            

            
        public IActionResult BoyahaneDashboard(DateTime? raporTarihi)
            
        {
            
            var islenecekTarih = raporTarihi ?? DateTime.Today;
            
            string rootPath = _hostingEnvironment.WebRootPath;
            
            string uretimDosyaYolu = Path.Combine(rootPath, "EXCELS", "YENİ BOYA GÜNLÜK VERİ TAKİP 2026 YILI.xlsm");
            
            string hataDosyaYolu = Path.Combine(rootPath, "EXCELS", "BOYA HATALI  PARÇA GİRİŞİ.xlsm");
            

            
            var viewModel = new BoyaDashboardViewModel();
            

            
            // Üretim verilerini oku
            
            var uretimListesi = new List<BoyaUretimSatir>();
            
            if (System.IO.File.Exists(uretimDosyaYolu))
            
            {
            
                using (var package = new ExcelPackage(new FileInfo(uretimDosyaYolu)))
            
                {
            
                    var worksheet = package.Workbook.Worksheets["VERİ KAYIT"];
            
                    if (worksheet != null)
            
                    {
            
                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            
                        {
            
                            
            
                                                        try
            
                                                        {
            
                                                            var dateValue = worksheet.Cells[row, 1].Value;
            
                                                            var dateString = dateValue?.ToString() ?? string.Empty;
            
                                                            uretimListesi.Add(new BoyaUretimSatir
            
                                                            {
            
                                                                Tarih = ParseTurkishDate(dateString),
            
                                                                PanelAdet = Convert.ToInt32(worksheet.Cells[row, 4].Value), // D sütunu
            
                                                                DosemeAdet = Convert.ToInt32(worksheet.Cells[row, 6].Value) // F sütunu
            
                                                            });
            
                                                        }
            
                            catch (Exception ex)
            
                            {
            
                                _logger.LogError($"Üretim Excel, Satır {row} okunurken hata: {ex.Message}");
            
                            }
            
                        }
            
                    }
            
                }
            
            }
            

            
            // Hata verilerini oku
            
            var hataListesi = new List<BoyaHataSatir>();
            
            if (System.IO.File.Exists(hataDosyaYolu))
            
            {
            
                using (var package = new ExcelPackage(new FileInfo(hataDosyaYolu)))
            
                {
            
                    var worksheet = package.Workbook.Worksheets["VERİ KAYIT"];
            
                    if (worksheet != null)
            
                    {
            
                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            
                        {
            
                            
            
                                                        try
            
                                                        {
            
                                                            var dateValue = worksheet.Cells[row, 1].Value;
            
                                                            var dateString = dateValue?.ToString() ?? string.Empty;
            
                                                            hataListesi.Add(new BoyaHataSatir
            
                                                            {
            
                                                                Tarih = ParseTurkishDate(dateString),
            
                                                                HataNedeni = worksheet.Cells[row, 7].Value?.ToString()?.Trim() ?? "Bilinmeyen", // G sütunu
            
                                                                HataliAdet = Convert.ToInt32(worksheet.Cells[row, 5].Value) // E sütunu
            
                                                            });
            
                                                        }
            
                            catch (Exception ex)
            
                            {
            
                                _logger.LogError($"Hata Excel, Satır {row} okunurken hata: {ex.Message}");
            
                            }
            
                        }
            
                    }
            
                }
            
            }
            

            
            // KPI'ları Hesapla
            
            var gununUretimVerileri = uretimListesi.Where(x => x.Tarih.Date == islenecekTarih.Date).ToList();
            
            var gununHataVerileri = hataListesi.Where(x => x.Tarih.Date == islenecekTarih.Date).ToList();
            

            
            viewModel.GunlukToplamBoyama = gununUretimVerileri.Sum(x => x.PanelAdet + x.DosemeAdet);
            
            viewModel.GunlukHataSayisi = gununHataVerileri.Sum(x => x.HataliAdet);
            

            
            if (viewModel.GunlukToplamBoyama > 0)
            
            {
            
                viewModel.FireOrani = (viewModel.GunlukHataSayisi / (viewModel.GunlukToplamBoyama + viewModel.GunlukHataSayisi)) * 100;
            
            }
            
            else
            
            {
            
                viewModel.FireOrani = 0;
            
            }
            

            

            
            // Grafik Verilerini Hazırla
            
            var hataGruplari = gununHataVerileri
            
                .GroupBy(x => x.HataNedeni)
            
                .Select(g => new { Neden = g.Key, Toplam = g.Sum(x => x.HataliAdet) })
            
                .OrderByDescending(x => x.Toplam)
            
                .ToList();
            

            
            viewModel.HataNedenleriListesi = hataGruplari.Select(x => x.Neden).Where(n => n != null).ToList()!;
            
            viewModel.HataSayilariListesi = hataGruplari.Select(x => x.Toplam).ToList();
            

            
            
            

            
                        return View(viewModel);
            

            
                    }
            

            
            
            

            
                    private DateTime ParseTurkishDate(string dateString)
            

            
                    {
            

            
                        if (string.IsNullOrWhiteSpace(dateString))
            

            
                        {
            

            
                            return DateTime.MinValue;
            

            
                        }
            

            
            
            

            
                        var monthMap = new Dictionary<string, int>
            

            
                        {
            

            
                            { "ocak", 1 }, { "şubat", 2 }, { "mart", 3 }, { "nisan", 4 },
            

            
                            { "mayıs", 5 }, { "haziran", 6 }, { "temmuz", 7 }, { "ağustos", 8 },
            

            
                            { "eylül", 9 }, { "ekim", 10 }, { "kasım", 11 }, { "aralık", 12 }
            

            
                        };
            

            
            
            

            
                        var dayNames = new[] { "pazartesi", "salı", "çarşamba", "perşembe", "cuma", "cumartesi", "pazar" };
            

            
            
            

            
                        var cleanString = dateString.ToLowerInvariant();
            

            
                        foreach (var dayName in dayNames)
            

            
                        {
            

            
                            cleanString = cleanString.Replace(dayName, string.Empty);
            

            
                        }
            

            
            
            

            
                        var parts = cleanString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            

            
                        if (parts.Length < 3)
            

            
                        {
            

            
                             // Eğer format OA Date ise, doğrudan çevirmeyi dene
            

            
                            if (double.TryParse(dateString, out double oaDate))
            

            
                            {
            

            
                                return DateTime.FromOADate(oaDate);
            

            
                            }
            

            
                            return DateTime.MinValue; // Veya bir hata fırlat
            

            
                        }
            

            
            
            

            
                        try
            

            
                        {
            

            
                            var day = int.Parse(parts[0]);
            

            
                            var month = monthMap[parts[1]];
            

            
                            var year = int.Parse(parts[2]);
            

            
            
            

            
                            return new DateTime(year, month, day);
            

            
                        }
            

            
                        catch (Exception ex)
            

            
                        {
            

            
                            _logger.LogError($"Tarih çevirme hatası: '{dateString}'. Hata: {ex.Message}");
            

            
                            // Tarih çevrilemezse, OA Date formatını dene
            

            
                            if (double.TryParse(dateString, out double oaDate))
            

            
                            {
            

            
                                return DateTime.FromOADate(oaDate);
            

            
                            }
            

            
                            return DateTime.MinValue; // Veya uygun bir varsayılan değer
            

            
                        }
            

            
                    }
            

            
            
            

            
                    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            

            
                    public IActionResult Error()
            

            
                    {
            

            
                        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            

            
                    }
            

            
                }
            

            
            }