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

        public IActionResult ProfilLazerVerileri(DateTime? raporTarihi, int? ay, int? yil)
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
                UrunHarcananSure = new List<int>(),
                HataNedenleri = new List<string>(),
                HataNedenAdetleri = new List<int>(),
                HataUrunSonuclari = new List<string>(),
                HataUrunSonucAdetleri = new List<int>()
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
                            UretimAdedi = ParseUretimAdedi(worksheet.Cells[row, 5].Value),
                            CalismaSuresi = ParseCalismaSuresiDakika(worksheet.Cells[row, 6].Value)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Satır {row} okunurken hata: {ex.Message}");
                        // Hatalı satırları atla veya bir hata mesajı göster
                    }
                }
            }

            // Profil Lazer Hatalı Parça Verileri
            var hataExcelData = new List<ProfilHataSatir>();
            string hataFilePath = Path.Combine(rootPath, "EXCELS", "METAL HATALI  PARÇA GİRİŞİ.xlsm");
            if (System.IO.File.Exists(hataFilePath))
            {
                using (var package = new ExcelPackage(new FileInfo(hataFilePath)))
                {
                    var worksheet = package.Workbook.Worksheets["VERİ KAYIT"];
                    if (worksheet != null)
                    {
                        int rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var dateValue = worksheet.Cells[row, 1].Value;
                                var dateString = dateValue?.ToString() ?? string.Empty;
                                var bolumAdi = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                                var hataUrunSonucu = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                                var hataNedeni = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                                var adetValue = worksheet.Cells[row, 5].Value;

                                hataExcelData.Add(new ProfilHataSatir
                                {
                                    Tarih = ParseTurkishDate(dateString),
                                    BolumAdi = bolumAdi,
                                    HataUrunSonucu = hataUrunSonucu,
                                    HataNedeni = hataNedeni,
                                    Adet = ParseUretimAdedi(adetValue)
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

            // Adım 2.2: Hesaplama (KPI'lar için)
            var gununVerileri = excelData.AsQueryable();

            if (ay.HasValue && yil.HasValue)
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

            // Hatalı ürün KPI + Hata nedenleri grafik verisi
            var profilHataVerileri = hataExcelData
                .Where(x => x.Tarih != DateTime.MinValue)
                .Where(x =>
                {
                    var bolum = (x.BolumAdi ?? "").ToLowerInvariant();
                    return bolum.Contains("metal") || bolum.Contains("profil") || bolum.Contains("lazer");
                })
                .AsQueryable();

            if (ay.HasValue && yil.HasValue)
            {
                profilHataVerileri = profilHataVerileri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
            }
            else
            {
                profilHataVerileri = profilHataVerileri.Where(x => x.Tarih.Date == islenecekTarih.Date);
            }

            viewModel.HataliUrunAdedi = profilHataVerileri.Sum(x => x.Adet);
            viewModel.HurdaAdedi = profilHataVerileri
                .Where(x => (x.HataUrunSonucu ?? "").ToLowerInvariant().Contains("hurda"))
                .Sum(x => x.Adet);

            var hataNedenGruplari = profilHataVerileri
                .GroupBy(x => NormalizeLabel(x.HataNedeni))
                .Select(g => new { Neden = g.Key, Toplam = g.Sum(x => x.Adet) })
                .OrderByDescending(x => x.Toplam)
                .ToList();

            viewModel.HataNedenleri = hataNedenGruplari.Select(x => x.Neden).ToList();
            viewModel.HataNedenAdetleri = hataNedenGruplari.Select(x => x.Toplam).ToList();

            var hataUrunGruplari = profilHataVerileri
                .GroupBy(x => NormalizeLabel(x.HataUrunSonucu))
                .Select(g => new { Sonuc = g.Key, Toplam = g.Sum(x => x.Adet) })
                .OrderByDescending(x => x.Toplam)
                .ToList();

            viewModel.HataUrunSonuclari = hataUrunGruplari.Select(x => x.Sonuc).ToList();
            viewModel.HataUrunSonucAdetleri = hataUrunGruplari.Select(x => x.Toplam).ToList();

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

            DateTime trendBaslangic;
            DateTime trendBitis;

            if (ay.HasValue && yil.HasValue)
            {
                trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
                trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
                ViewBag.TrendTitle = "Aylık Üretim Trendi";
            }
            else
            {
                var referansTarih = raporTarihi ?? DateTime.Today;
                trendBaslangic = referansTarih.AddDays(-6);
                trendBitis = referansTarih;
                ViewBag.TrendTitle = "Son 7 Günlük Üretim Trendi";
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
                .Where(x => x.Tarih != DateTime.MinValue)
                .Where(x =>
                {
                    var bolum = (x.BolumAdi ?? "").ToLowerInvariant();
                    return bolum.Contains("metal") || bolum.Contains("profil") || bolum.Contains("lazer");
                })
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Adet));

            viewModel.GunlukHataliUrunSayilari = tumTarihler.Select(t => hataTrendVerileri.TryGetValue(t, out var toplam) ? toplam : 0).ToList();
            
                        return View(viewModel);
                    }
            

            
        public IActionResult BoyahaneDashboard(DateTime? raporTarihi, int? ay, int? yil)
            
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
            
            var gununUretimVerileri = uretimListesi.AsQueryable();
            var gununHataVerileri = hataListesi.AsQueryable();

            if (ay.HasValue && yil.HasValue)
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

            if (viewModel.GunlukToplamBoyama > 0)
            {
                viewModel.FireOrani = (viewModel.GunlukHataSayisi / viewModel.GunlukToplamBoyama) * 100;
            }
            else
            {
                viewModel.FireOrani = 0;
            }
            

            

            
            // Grafik Verilerini Hazırla
            
            // Hata Nedenleri (Pasta Grafik)
            var hataGruplari = gununHataVerileri
                .GroupBy(x => x.HataNedeni)
                .Select(g => new { Neden = g.Key, Toplam = g.Sum(x => x.HataliAdet) })
                .OrderByDescending(x => x.Toplam)
                .ToList();
            

            
            viewModel.HataNedenleriListesi = hataGruplari.Select(x => x.Neden).Where(n => n != null).ToList()!;
            viewModel.HataSayilariListesi = hataGruplari.Select(x => x.Toplam).ToList();

            DateTime trendBaslangic;
            DateTime trendBitis;
            if (ay.HasValue && yil.HasValue)
            {
                trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
                trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
                ViewBag.UretimDagilimiTitle = "Aylık Üretim Dağılımı (Panel vs Döşeme)";
                ViewBag.KaliteTrendTitle = "Kalite Trendi (Aylık)";
                ViewBag.UretimTrendTitle = "Üretim Trendi (Aylık)";
            }
            else
            {
                var referansTarih = raporTarihi ?? DateTime.Today;
                trendBaslangic = referansTarih.AddDays(-6);
                trendBitis = referansTarih;
                ViewBag.UretimDagilimiTitle = "Üretim Dağılımı (Panel vs Döşeme)";
                ViewBag.KaliteTrendTitle = "Kalite Trendi (Son 7 Gün)";
                ViewBag.UretimTrendTitle = "Üretim Trendi (Son 7 Gün)";
            }

            var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
                .Select(offset => trendBaslangic.Date.AddDays(offset))
                .ToList();

            // Üretim Dağılımı (Stacked Bar)
            var uretimDagilimi = uretimListesi
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .Select(g => new {
                    Tarih = g.Key,
                    Panel = g.Sum(x => x.PanelAdet),
                    Doseme = g.Sum(x => x.DosemeAdet)
                })
                .OrderBy(x => x.Tarih)
                .ToDictionary(x => x.Tarih, x => x);

            viewModel.UretimDagilimi.Labels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
            viewModel.UretimDagilimi.PanelData = tumTarihler.Select(t =>
                uretimDagilimi.TryGetValue(t, out var v) ? v.Panel : 0
            ).ToList();
            viewModel.UretimDagilimi.DosemeData = tumTarihler.Select(t =>
                uretimDagilimi.TryGetValue(t, out var v) ? v.Doseme : 0
            ).ToList();

            // Kalite Trendi (Çizgi Grafik)
            var hataDagilimi = hataListesi
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .Select(g => new { Tarih = g.Key, ToplamHata = g.Sum(x => x.HataliAdet) })
                .OrderBy(x => x.Tarih)
                .ToDictionary(x => x.Tarih, x => x.ToplamHata);

            viewModel.KaliteTrendi.Labels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
            viewModel.KaliteTrendi.Data = tumTarihler.Select(t =>
                hataDagilimi.TryGetValue(t, out var toplam) ? toplam : 0
            ).ToList();

            // Üretim Trendi (Çizgi Grafik)
            var uretimTrend = uretimListesi
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .Select(g => new { Tarih = g.Key, ToplamUretim = g.Sum(x => x.PanelAdet + x.DosemeAdet) })
                .OrderBy(x => x.Tarih)
                .ToDictionary(x => x.Tarih, x => x.ToplamUretim);
            
            viewModel.UretimTrendi.Labels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
            viewModel.UretimTrendi.Data = tumTarihler.Select(t =>
                uretimTrend.TryGetValue(t, out var toplam) ? toplam : 0
            ).ToList();
            

                        return View(viewModel);
            

        }

        public IActionResult PvcDashboard(DateTime? raporTarihi, int? ay, int? yil)
        {
            var islenecekTarih = raporTarihi ?? DateTime.Today;

            var viewModel = new PvcDashboardViewModel
            {
                RaporTarihi = islenecekTarih
            };

            string rootPath = _hostingEnvironment.WebRootPath;
            string filePath = Path.Combine(rootPath, "EXCELS", "PVC BÖLÜMÜ VERİ EKRANI 2026.xlsm");

            if (!System.IO.File.Exists(filePath))
            {
                ViewBag.ErrorMessage = "Excel dosyası bulunamadı: " + filePath;
                return View(viewModel);
            }

            var excelData = new List<PvcSatirModel>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets["KAYIT"];
                if (worksheet == null)
                {
                    ViewBag.ErrorMessage = "'KAYIT' sayfası bulunamadı.";
                    return View(viewModel);
                }

                int rowCount = worksheet.Dimension.Rows;
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var dateValue = worksheet.Cells[row, 1].Value;
                        var dateString = dateValue?.ToString() ?? string.Empty;

                        excelData.Add(new PvcSatirModel
                        {
                            Tarih = ParseTurkishDate(dateString),
                            Makine = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                            UretimMetraj = ParseDoubleCell(worksheet.Cells[row, 3].Value),
                            ParcaSayisi = ParseDoubleCell(worksheet.Cells[row, 4].Value),
                            CalismaKosulu = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                            Duraklama1 = ParseDoubleCell(worksheet.Cells[row, 6].Value),
                            DuraklamaNedeni1 = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                            Duraklama2 = ParseDoubleCell(worksheet.Cells[row, 8].Value),
                            DuraklamaNedeni2 = worksheet.Cells[row, 9].Value?.ToString()?.Trim(),
                            Duraklama3 = ParseDoubleCell(worksheet.Cells[row, 10].Value),
                            DuraklamaNedeni3 = worksheet.Cells[row, 11].Value?.ToString()?.Trim(),
                            Aciklama = worksheet.Cells[row, 12].Value?.ToString()?.Trim(),
                            UretimOrani = ParsePercentCell(worksheet.Cells[row, 13].Value),
                            KayipSure = ParsePercentCell(worksheet.Cells[row, 14].Value),
                            FiiliCalismaOrani = ParsePercentCell(worksheet.Cells[row, 15].Value)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"PVC Excel, Satır {row} okunurken hata: {ex.Message}");
                    }
                }
            }

            excelData = excelData.Where(x => x.Tarih != DateTime.MinValue).ToList();

            var filtreliVeri = excelData.AsQueryable();
            if (ay.HasValue && yil.HasValue)
            {
                filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
                ViewBag.UretimTrendTitle = "Üretim Trendi (Aylık)";
                ViewBag.FiiliCalismaTrendTitle = "Fiili Çalışma Oranı (Aylık)";
                ViewBag.KayipSureTrendTitle = "Kayıp Süre (Aylık)";
            }
            else
            {
                filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
                ViewBag.UretimTrendTitle = "Üretim Trendi (Son 7 Gün)";
                ViewBag.FiiliCalismaTrendTitle = "Fiili Çalışma Oranı (Son 7 Gün)";
                ViewBag.KayipSureTrendTitle = "Kayıp Süre (Son 7 Gün)";
            }

            viewModel.ToplamUretimMetraj = filtreliVeri.Sum(x => x.UretimMetraj);
            viewModel.ToplamParcaSayisi = filtreliVeri.Sum(x => x.ParcaSayisi);
            viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3);
            viewModel.OrtalamaFiiliCalismaOrani = filtreliVeri.Any() ? filtreliVeri.Average(x => x.FiiliCalismaOrani) : 0;

            DateTime trendBaslangic;
            DateTime trendBitis;
            if (ay.HasValue && yil.HasValue)
            {
                trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
                trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
            }
            else
            {
                var referansTarih = raporTarihi ?? DateTime.Today;
                trendBaslangic = referansTarih.AddDays(-6);
                trendBitis = referansTarih;
            }

            var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
                .Select(offset => trendBaslangic.Date.AddDays(offset))
                .ToList();

            var uretimGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.UretimMetraj));

            viewModel.UretimTrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
            viewModel.UretimTrendData = tumTarihler.Select(t => uretimGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

            var fiiliGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Average(x => x.FiiliCalismaOrani));

            viewModel.FiiliCalismaLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
            viewModel.FiiliCalismaData = tumTarihler.Select(t => fiiliGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

            var kayipGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Average(x => x.KayipSure));

            viewModel.KayipSureData = tumTarihler.Select(t => kayipGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

            var makineGruplari = filtreliVeri
                .GroupBy(x => x.Makine ?? "Bilinmeyen")
                .Select(g => new { Makine = g.Key, Metraj = g.Sum(x => x.UretimMetraj), Parca = g.Sum(x => x.ParcaSayisi) })
                .OrderByDescending(x => x.Metraj)
                .ToList();

            viewModel.MakineLabels = makineGruplari.Select(x => x.Makine).ToList();
            viewModel.MakineUretimData = makineGruplari.Select(x => x.Metraj).ToList();
            viewModel.MakineParcaData = makineGruplari.Select(x => x.Parca).ToList();

            var duraklamaNedenleri = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in filtreliVeri)
            {
                AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
                AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
                AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
            }

            var duraklamaList = duraklamaNedenleri
                .OrderByDescending(x => x.Value)
                .ToList();

            viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
            viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

            return View(viewModel);
        }

        public IActionResult MasterwoodDashboard(DateTime? raporTarihi, int? ay, int? yil)
        {
            var islenecekTarih = raporTarihi ?? DateTime.Today;

            var viewModel = new MasterwoodDashboardViewModel
            {
                RaporTarihi = islenecekTarih
            };

            string rootPath = _hostingEnvironment.WebRootPath;
            string filePath = Path.Combine(rootPath, "EXCELS", "MARWOOD Masterwood Veri Ekranı.xlsm");

            if (!System.IO.File.Exists(filePath))
            {
                ViewBag.ErrorMessage = "Excel dosyası bulunamadı: " + filePath;
                return View(viewModel);
            }

            var excelData = new List<MasterwoodSatirModel>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets["ANA RAPOR"];
                if (worksheet == null)
                {
                    ViewBag.ErrorMessage = "'ANA RAPOR' sayfası bulunamadı.";
                    return View(viewModel);
                }

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;
                int duraklama2Col = FindColumn(worksheet, "DURAKLAMA ZAMANI 2 (DK)", "DURAKLAMA ZAMANI 2", "DURAKLAMA2");
                int duraklamaNeden2Col = FindColumn(worksheet, "DURAKLAMA NEDENİ 2", "DURAKLAMA NEDENI 2");
                int duraklama3Col = FindColumn(worksheet, "DURAKLAMA ZAMANI 3 (DK)", "DURAKLAMA ZAMANI 3", "DURAKLAMA3");
                int duraklamaNeden3Col = FindColumn(worksheet, "DURAKLAMA NEDENİ 3", "DURAKLAMA NEDENI 3");
                int uretimOraniCol = FindColumn(worksheet, "ÜRETİM ORANI", "URETIM ORANI", "ÜRETİMORANI", "URETIMORANI");
                int kayipSureCol = FindColumn(worksheet, "KAYIP SÜRE", "KAYIP SURE", "KAYIPSURE");
                int fiiliCalismaCol = FindColumn(worksheet, "FİİLİ ÇALIŞMA ORANI", "FIILI CALISMA ORANI", "FIILI CALISMAORANI", "FİİLİ ÇALIŞMA");

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var dateValue = worksheet.Cells[row, 1].Value;
                        var dateString = dateValue?.ToString() ?? string.Empty;

                        var duraklama2 = duraklama2Col > 0
                            ? ParseDoubleCell(worksheet.Cells[row, duraklama2Col].Value)
                            : (colCount >= 8 ? ParseDoubleCell(worksheet.Cells[row, 8].Value) : 0);
                        var duraklamaNeden2 = duraklamaNeden2Col > 0
                            ? worksheet.Cells[row, duraklamaNeden2Col].Value?.ToString()?.Trim()
                            : (colCount >= 9 ? worksheet.Cells[row, 9].Value?.ToString()?.Trim() : null);
                        var duraklama3 = duraklama3Col > 0
                            ? ParseDoubleCell(worksheet.Cells[row, duraklama3Col].Value)
                            : (colCount >= 10 ? ParseDoubleCell(worksheet.Cells[row, 10].Value) : 0);
                        var duraklamaNeden3 = duraklamaNeden3Col > 0
                            ? worksheet.Cells[row, duraklamaNeden3Col].Value?.ToString()?.Trim()
                            : (colCount >= 11 ? worksheet.Cells[row, 11].Value?.ToString()?.Trim() : null);

                        var uretimOrani = uretimOraniCol > 0
                            ? ParsePercentCell(worksheet.Cells[row, uretimOraniCol].Value)
                            : (colCount >= 13 ? ParsePercentCell(worksheet.Cells[row, 13].Value) : 0);
                        var kayipSureOrani = kayipSureCol > 0
                            ? ParsePercentCell(worksheet.Cells[row, kayipSureCol].Value)
                            : (colCount >= 14 ? ParsePercentCell(worksheet.Cells[row, 14].Value) : 0);
                        var fiiliCalismaOrani = fiiliCalismaCol > 0
                            ? ParsePercentCell(worksheet.Cells[row, fiiliCalismaCol].Value)
                            : (colCount >= 15 ? ParsePercentCell(worksheet.Cells[row, 15].Value) : 0);

                        excelData.Add(new MasterwoodSatirModel
                        {
                            Tarih = ParseTurkishDate(dateString),
                            KisiSayisi = ParseDoubleCell(worksheet.Cells[row, 2].Value),
                            DelikSayisi = ParseDoubleCell(worksheet.Cells[row, 3].Value),
                            DelikFreezeSayisi = ParseDoubleCell(worksheet.Cells[row, 4].Value),
                            CalismaKosulu = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                            Duraklama1 = ParseDoubleCell(worksheet.Cells[row, 6].Value),
                            DuraklamaNedeni1 = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                            Duraklama2 = duraklama2,
                            DuraklamaNedeni2 = duraklamaNeden2,
                            Duraklama3 = duraklama3,
                            DuraklamaNedeni3 = duraklamaNeden3,
                            UretimOrani = NormalizePercentValue(uretimOrani),
                            KayipSureOrani = NormalizePercentValue(kayipSureOrani),
                            FiiliCalismaOrani = NormalizePercentValue(fiiliCalismaOrani)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"MASTERWOOD Excel, Satır {row} okunurken hata: {ex.Message}");
                    }
                }
            }

            excelData = excelData.Where(x => x.Tarih != DateTime.MinValue).ToList();

            var filtreliVeri = excelData.AsQueryable();
            if (ay.HasValue && yil.HasValue)
            {
                filtreliVeri = filtreliVeri.Where(x => x.Tarih.Month == ay.Value && x.Tarih.Year == yil.Value);
                ViewBag.MasterwoodTrendTitle = "Üretim Trendi (Aylık)";
                ViewBag.KisiTrendTitle = "Kişi Sayısı (Aylık)";
            }
            else
            {
                filtreliVeri = filtreliVeri.Where(x => x.Tarih.Date == islenecekTarih.Date);
                ViewBag.MasterwoodTrendTitle = "Üretim Trendi (Son 7 Gün)";
                ViewBag.KisiTrendTitle = "Kişi Sayısı (Son 7 Gün)";
            }

            viewModel.ToplamDelik = filtreliVeri.Sum(x => x.DelikSayisi);
            viewModel.ToplamDelikFreeze = filtreliVeri.Sum(x => x.DelikFreezeSayisi);
            viewModel.OrtalamaKisiSayisi = filtreliVeri.Any() ? filtreliVeri.Average(x => x.KisiSayisi) : 0;
            viewModel.ToplamDuraklamaDakika = filtreliVeri.Sum(x => x.Duraklama1 + x.Duraklama2 + x.Duraklama3);

            DateTime trendBaslangic;
            DateTime trendBitis;
            if (ay.HasValue && yil.HasValue)
            {
                trendBaslangic = new DateTime(yil.Value, ay.Value, 1);
                trendBitis = trendBaslangic.AddMonths(1).AddDays(-1);
            }
            else
            {
                var referansTarih = raporTarihi ?? DateTime.Today;
                trendBaslangic = referansTarih.AddDays(-6);
                trendBitis = referansTarih;
            }

            var tumTarihler = Enumerable.Range(0, (trendBitis.Date - trendBaslangic.Date).Days + 1)
                .Select(offset => trendBaslangic.Date.AddDays(offset))
                .ToList();

            var delikGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikSayisi));

            var delikFreezeGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.DelikFreezeSayisi));

            var kisiGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Average(x => x.KisiSayisi));

            viewModel.TrendLabels = tumTarihler.Select(t => t.ToString("dd.MM")).ToList();
            viewModel.DelikTrendData = tumTarihler.Select(t => delikGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
            viewModel.DelikFreezeTrendData = tumTarihler.Select(t => delikFreezeGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
            viewModel.KisiTrendData = tumTarihler.Select(t => kisiGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

            var uretimOraniGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Average(x => x.UretimOrani));

            var kayipSureGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Average(x => x.KayipSureOrani));

            var fiiliCalismaGunluk = excelData
                .Where(x => x.Tarih.Date >= trendBaslangic.Date && x.Tarih.Date <= trendBitis.Date)
                .GroupBy(x => x.Tarih.Date)
                .ToDictionary(g => g.Key, g => g.Average(x => x.FiiliCalismaOrani));

            viewModel.UretimOraniTrendData = tumTarihler.Select(t => uretimOraniGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
            viewModel.KayipSureTrendData = tumTarihler.Select(t => kayipSureGunluk.TryGetValue(t, out var v) ? v : 0).ToList();
            viewModel.FiiliCalismaTrendData = tumTarihler.Select(t => fiiliCalismaGunluk.TryGetValue(t, out var v) ? v : 0).ToList();

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
                AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni1, row.Duraklama1);
                AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni2, row.Duraklama2);
                AddDuraklama(duraklamaNedenleri, row.DuraklamaNedeni3, row.Duraklama3);
            }

            var duraklamaList = duraklamaNedenleri
                .OrderByDescending(x => x.Value)
                .ToList();

            viewModel.DuraklamaNedenLabels = duraklamaList.Select(x => x.Key).ToList();
            viewModel.DuraklamaNedenData = duraklamaList.Select(x => x.Value).ToList();

            return View(viewModel);
        }
        private static int FindColumn(ExcelWorksheet worksheet, params string[] headers)
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

        private static string NormalizeHeaderForMatch(string value)
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
        private DateTime ParseTurkishDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return DateTime.MinValue;
            }

            var trimmed = dateString.Trim();
            var trCulture = new System.Globalization.CultureInfo("tr-TR");

            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double oaDate))
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

            if (DateTime.TryParse(trimmed, trCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var parsedDate))
            {
                return parsedDate.Date;
            }

            if (DateTime.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out parsedDate))
            {
                return parsedDate.Date;
            }

            if (trimmed.Length == 8 && long.TryParse(trimmed, out _))
            {
                if (DateTime.TryParseExact(trimmed, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    return parsedDate.Date;
                }
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
            if (parts.Length >= 2)
            {
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
                catch (Exception ex)
                {
                    _logger.LogError($"Tarih çevirme hatası: '{dateString}'. Hata: {ex.Message}");
                    return DateTime.MinValue;
                }
            }

            _logger.LogError($"Tarih çevirme hatası: '{dateString}'. Hata: format tanınamadı");
            return DateTime.MinValue;
        }

        private static double ParseDoubleCell(object? value)
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

            var trCulture = new System.Globalization.CultureInfo("tr-TR");
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, trCulture, out var result))
            {
                return result;
            }

            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return 0;
        }

        private static double ParsePercentCell(object? value)
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
            var trCulture = new System.Globalization.CultureInfo("tr-TR");
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, trCulture, out var result))
            {
                return result;
            }

            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return 0;
        }

        private int ParseUretimAdedi(object? value)
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

            var normalized = text.ToLowerInvariant();
            normalized = normalized.Replace(",", "."); // 1,5 -> 1.5

            var match = System.Text.RegularExpressions.Regex.Match(normalized, @"-?\d+(\.\d+)?");
            if (match.Success && double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var number))
            {
                return (int)Math.Round(number, MidpointRounding.AwayFromZero);
            }

            if (normalized.Contains("yarım"))
            {
                return 1; // yarım boy/yarım parça gibi ifadeler için yuvarlama
            }

            return 0;
        }

        private int ParseCalismaSuresiDakika(object? value)
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
                var hourMatch = System.Text.RegularExpressions.Regex.Match(normalized, @"(\d+(\.\d+)?)saat");
                if (hourMatch.Success && double.TryParse(hourMatch.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var hours))
                {
                    totalMinutes += hours * 60;
                }
            }

            if (normalized.Contains("dk"))
            {
                var minMatch = System.Text.RegularExpressions.Regex.Match(normalized, @"(\d+(\.\d+)?)dk");
                if (minMatch.Success && double.TryParse(minMatch.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mins))
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

            var fallbackMatch = System.Text.RegularExpressions.Regex.Match(normalized, @"-?\d+(\.\d+)?");
            if (fallbackMatch.Success && double.TryParse(fallbackMatch.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fallback))
            {
                return (int)Math.Round(fallback, MidpointRounding.AwayFromZero);
            }

            return 0;
        }

        private static void AddDuraklama(Dictionary<string, double> toplamlar, string? neden, double dakika)
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

            if (toplamlar.ContainsKey(neden))
            {
                toplamlar[neden] += dakika;
            }
            else
            {
                toplamlar[neden] = dakika;
            }
        }

        private static double NormalizePercentValue(double value)
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

        private static string NormalizeLabel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Bilinmeyen";
            }

            var trimmed = value.Trim();
            var lower = trimmed.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            var textInfo = new System.Globalization.CultureInfo("tr-TR").TextInfo;
            return textInfo.ToTitleCase(lower);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            

            
                    public IActionResult Error()
            

            
                    {
            

            
                        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            

            
                    }
            

            
                }
            

            
            }
