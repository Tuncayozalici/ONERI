using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Models;
using Microsoft.AspNetCore.Authorization;

namespace ONERI.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FabrikaContext _context;

        public AdminController(FabrikaContext context)
        {
            _context = context;
        }

        // Görev 1: Listeleme (Index Metodu)
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
        public async Task<IActionResult> BolumYoneticileri()
        {
            var yoneticiler = await _context.BolumYoneticileri.OrderBy(y => y.BolumAdi).ToListAsync();
            // The form will be on this view, so we can pass a new BolumYonetici model for it.
            ViewBag.YeniYonetici = new BolumYonetici();
            return View(yoneticiler);
        }

        // B. Yeni Sorumlu Ekleme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YoneticiEkle(BolumYonetici bolumYonetici)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.BolumYoneticileri.FirstOrDefaultAsync(y => y.BolumAdi.ToLower() == bolumYonetici.BolumAdi.ToLower());
                if (existing != null)
                {
                    // Redirecting will lose the error message, but it's the simplest approach without a ViewModel.
                    // A TempData message could be used to show the error after redirection.
                    TempData["YoneticiHata"] = "Bu bölüm için zaten bir yönetici atanmış.";
                    return RedirectToAction(nameof(BolumYoneticileri));
                }

                _context.Add(bolumYonetici);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(BolumYoneticileri));
            }

            // If model state is invalid (e.g. required field missing), redirect back.
            // Again, specific errors are lost.
            return RedirectToAction(nameof(BolumYoneticileri));
        }

        // C. Sorumlu Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YoneticiSil(int id)
        {
            var yonetici = await _context.BolumYoneticileri.FindAsync(id);
            if (yonetici == null)
            {
                return NotFound();
            }

            _context.BolumYoneticileri.Remove(yonetici);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(BolumYoneticileri));
        }

        #endregion

        // Görev 4: Detay Sayfası
        [HttpGet]
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
    }
}