using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ONERI.Data;
using ONERI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ONERI.Models.Authorization;

namespace ONERI.Controllers
{
    public class OneriController : Controller
    {
        private readonly FabrikaContext _context;

        public OneriController(FabrikaContext context)
        {
            _context = context;
        }

        // GET: Oneri/Index
        // Bu metot, kullanıcıyı doğrudan yeni öneri oluşturma formuna yönlendirir.
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Yeni));
        }

        // GET: Oneri/Tesekkurler?token={trackingToken}
        // Başarılı bir gönderim sonrası gösterilecek sayfa.
        [Authorize(Policy = Permissions.Oneri.Create)]
        public async Task<IActionResult> Tesekkurler(string token)
        {
            if (!Guid.TryParse(token, out var parsedToken))
            {
                return RedirectToAction(nameof(Yeni));
            }

            var oneri = await _context.Oneriler
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(o => o.TrackingToken == parsedToken);
            if (oneri == null)
            {
                return RedirectToAction(nameof(Yeni));
            }

            return View(oneri);
        }

        // GET: Oneri/Yeni
        [Authorize(Policy = Permissions.Oneri.Create)]
        public async Task<IActionResult> Yeni()
        {
            var bolumler = await _context.BolumYoneticileri
                                         .AsNoTracking()
                                         .Select(b => b.BolumAdi)
                                         .Distinct()
                                         .ToListAsync();
            ViewBag.Bolumler = new SelectList(bolumler);
            var model = new OneriCreateViewModel();
            return View(model);
        }

        // POST: Oneri/Yeni
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Oneri.Create)]
        public async Task<IActionResult> Yeni(OneriCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Haritalama: ViewModel'den ana modele veri aktarımı
                var oneri = new Oneri
                {
                    OnerenKisi = viewModel.OnerenKisi,
                    CalistigiBolum = viewModel.CalistigiBolum,
                    AltBolum = viewModel.AltBolum,
                    Bolum = viewModel.Bolum,
                    Konu = viewModel.Konu,
                    Aciklama = viewModel.Aciklama,
                    TrackingToken = Guid.NewGuid(),
                    // Bu alanlar sunucu tarafından, güvenli bir şekilde atanır
                    Tarih = DateTime.Now,
                    Durum = OneriDurum.Beklemede 
                };

                _context.Add(oneri);
                await _context.SaveChangesAsync();

                // İlgili bölüm yöneticisine e-posta gönderme mantığı...
                var bolum = (oneri.Bolum ?? "").ToLower();
                var yonetici = await _context.BolumYoneticileri
                                             .FirstOrDefaultAsync(y => y.BolumAdi != null && y.BolumAdi.ToLower() == bolum);

                if (yonetici != null)
                {
                    // E-posta gönderme işlemi burada yapılabilir
                }

                return RedirectToAction(nameof(Tesekkurler), new { token = oneri.TrackingToken });
            }
            
            // ModelState geçerli değilse, formu yeniden gösterirken Bölüm listesini tekrar doldur.
            var bolumler = await _context.BolumYoneticileri
                                         .AsNoTracking()
                                         .Select(b => b.BolumAdi)
                                         .Distinct()
                                         .ToListAsync();
            ViewBag.Bolumler = new SelectList(bolumler);
            return View(viewModel);
        }

        // Görev 1: Arama Kutusunu Gösterme (GET Metodu)
        [HttpGet]
        [Authorize(Policy = Permissions.Oneri.Query)]
        public async Task<IActionResult> Sorgula(string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                return await SorgulaByTrackingToken(token);
            }

            return View();
        }

        // Görev 2: Aramayı Yapma ve Sonucu Getirme (POST Metodu)
        [HttpPost]
        [ActionName("Sorgula")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Oneri.Query)]
        public async Task<IActionResult> SorgulaPost(string token)
        {
            return await SorgulaByTrackingToken(token);
        }

        private async Task<IActionResult> SorgulaByTrackingToken(string token)
        {
            ViewBag.SonToken = token;

            if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var parsedToken))
            {
                ViewBag.Hata = "Lütfen geçerli bir takip numarası giriniz.";
                return View();
            }

            // İlgili öneriyi ve varsa değerlendirmelerini birlikte çekiyoruz.
            var oneri = await _context.Oneriler
                                      .AsNoTracking()
                                      .Include(o => o.Degerlendirmeler) // Degerlendirmeler'i dahil et
                                      .FirstOrDefaultAsync(o => o.TrackingToken == parsedToken);

            if (oneri == null)
            {
                ViewBag.Hata = "Bu takip numarasına ait bir öneri bulunamadı.";
                return View(); // Boş arama sayfasını hata mesajıyla göster
            }

            // Kayıt bulunduysa, sonucu sayfaya model olarak gönder
            return View(oneri);
        }


        // 2. Değerlendirme Sayfasını Hazırla (Backend - GET)
        [HttpGet]
        [Authorize(Policy = Permissions.Oneri.Evaluate)]
        public async Task<IActionResult> Degerlendir(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var oneri = await _context.Oneriler.FindAsync(id);

            if (oneri == null || oneri.Durum != OneriDurum.Onaylandi)
            {
                // Eğer öneri bulunamazsa veya durumu değerlendirmeye uygun değilse,
                // kullanıcıyı bilgilendir veya anasayfaya yönlendir.
                // TempData ile bir mesaj göstermek iyi bir yaklaşım olabilir.
                TempData["Hata"] = "Değerlendirmeye uygun bir öneri bulunamadı.";
                return RedirectToAction("Index", "Admin");
            }

            var mevcutDegerlendirme = await _context.Degerlendirmeler.AnyAsync(d => d.OneriId == oneri.Id);
            if (mevcutDegerlendirme)
            {
                TempData["Hata"] = "Bu öneri daha önce değerlendirilmiş.";
                return RedirectToAction("Index", "Admin");
            }

            // Puanlama formu için boş bir Degerlendirme nesnesi oluştur.
            var degerlendirmeModel = new Degerlendirme
            {
                OneriId = oneri.Id
            };

            // Öneri bilgilerini de view'a taşıyalım ki ekranda gösterebilelim.
            ViewData["Oneri"] = oneri;

            return View(degerlendirmeModel);
        }

        // 4. Karar Mekanizması (Backend - POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Oneri.Evaluate)]
        public async Task<IActionResult> Degerlendir(Degerlendirme degerlendirme)
        {
            // ModelState.IsValid kontrolü, modeldeki zorunlu alanların (varsa) dolu geldiğini doğrular.
            // Puanların 0-25 aralığında olduğunu burada da kontrol etmek en güvenlisidir.
            if (!ModelState.IsValid)
            {
                // Formda bir hata varsa, formu tekrar göster.
                // Öneri bilgisini tekrar yükleyip göndermen gerekir.
                var oneriForView = await _context.Oneriler.FindAsync(degerlendirme.OneriId);
                ViewData["Oneri"] = oneriForView;
                return View(degerlendirme);
            }

            // Güvenlik Hesabı: Toplam puanı backend'de yeniden hesapla.
            degerlendirme.ToplamPuan = degerlendirme.GayretPuani +
                                     degerlendirme.OrijinallikPuani +
                                     degerlendirme.EtkiPuani +
                                     degerlendirme.UygulanabilirlikPuani;
            
            // İlgili öneriyi veritabanından bul.
            var oneri = await _context.Oneriler.FindAsync(degerlendirme.OneriId);
            if (oneri == null)
            {
                return NotFound(); // Öneri bulunamadıysa hata döndür.
            }

            var mevcutDegerlendirme = await _context.Degerlendirmeler.AnyAsync(d => d.OneriId == oneri.Id);
            if (mevcutDegerlendirme)
            {
                TempData["Hata"] = "Bu öneri daha önce değerlendirilmiş.";
                return RedirectToAction("Index", "Admin");
            }

            // BÜYÜK KARAR (İf-Else)
            if (degerlendirme.ToplamPuan >= 60)
            {
                oneri.Durum = OneriDurum.KabulEdildi; // Durumu "Kabul Edildi" yap.
            }
            else
            {
                oneri.Durum = OneriDurum.PuanlamaRed; // Durumu "Puanla Reddedildi" yap.
            }

            // Yeni oluşturulan değerlendirme kaydını veritabanına ekle.
            _context.Degerlendirmeler.Add(degerlendirme);

            try
            {
                // Hem Degerlendirmeler tablosuna eklemeyi, hem de Oneriler tablosundaki güncellemeyi kaydet.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Hata"] = "Bu öneri daha önce değerlendirilmiş.";
                return RedirectToAction("Index", "Admin");
            }

            // İşlem bittikten sonra yöneticiyi ana panele yönlendir.
            return RedirectToAction("Index", "Admin");
        }
    }
}
