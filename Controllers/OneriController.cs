using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ONERI.Data;
using ONERI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        // GET: Oneri/Tesekkurler
        // Başarılı bir gönderim sonrası gösterilecek sayfa.
        public IActionResult Tesekkurler()
        {
            return View();
        }

        // GET: Oneri/Yeni
        public async Task<IActionResult> Yeni()
        {
            var bolumler = await _context.BolumYoneticileri
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
                    // Bu alanlar sunucu tarafından, güvenli bir şekilde atanır
                    Tarih = DateTime.Now,
                    Durum = OneriDurum.Beklemede 
                };

                _context.Add(oneri);
                await _context.SaveChangesAsync();

                // İlgili bölüm yöneticisine e-posta gönderme mantığı...
                var yonetici = await _context.BolumYoneticileri
                                             .FirstOrDefaultAsync(y => y.BolumAdi.ToLower() == (oneri.Bolum ?? "").ToLower());

                if (yonetici != null)
                {
                    // E-posta gönderme işlemi burada yapılabilir
                }

                return View("Tesekkurler", oneri);
            }
            
            // ModelState geçerli değilse, formu yeniden gösterirken Bölüm listesini tekrar doldur.
            var bolumler = await _context.BolumYoneticileri
                                         .Select(b => b.BolumAdi)
                                         .Distinct()
                                         .ToListAsync();
            ViewBag.Bolumler = new SelectList(bolumler);
            return View(viewModel);
        }

        // Görev 1: Arama Kutusunu Gösterme (GET Metodu)
        [HttpGet]
        public IActionResult Sorgula()
        {
            return View();
        }

        // Görev 2: Aramayı Yapma ve Sonucu Getirme (POST Metodu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sorgula(int id)
        {
            if (id <= 0)
            {
                ViewBag.Hata = "Lütfen geçerli bir Öneri Numarası giriniz.";
                return View();
            }

            // İlgili öneriyi ve varsa değerlendirmelerini birlikte çekiyoruz.
            var oneri = await _context.Oneriler
                                      .Include(o => o.Degerlendirmeler) // Degerlendirmeler'i dahil et
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (oneri == null)
            {
                ViewBag.Hata = $"'{id}' numaralı bir öneri bulunamadı.";
                return View(); // Boş arama sayfasını hata mesajıyla göster
            }

            // Kayıt bulunduysa, sonucu sayfaya model olarak gönder
            return View(oneri);
        }


        // 2. Değerlendirme Sayfasını Hazırla (Backend - GET)
        [HttpGet]
        [Authorize(Roles = "Yönetici")]
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
        [Authorize(Roles = "Yönetici")]
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

            // Hem Degerlendirmeler tablosuna eklemeyi, hem de Oneriler tablosundaki güncellemeyi kaydet.
            await _context.SaveChangesAsync();

            // İşlem bittikten sonra yöneticiyi ana panele yönlendir.
            return RedirectToAction("Index", "Admin");
        }
    }
}
