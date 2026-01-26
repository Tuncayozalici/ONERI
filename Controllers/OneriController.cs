using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ONERI.Data;
using ONERI.Models;
using Microsoft.EntityFrameworkCore;

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
        // Bu metot, öneri formunu göstermek için kullanılır.
        public IActionResult Index()
        {
            return View();
        }

        // POST: Oneri/Index
        // Form gönderildiğinde bu metot çalışır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Oneri oneri)
        {
            // Modelin kurallara uygun olup olmadığını kontrol et (örn: zorunlu alanlar doldurulmuş mu?)
            if (ModelState.IsValid)
            {
                // Yeni öneriyi veritabanına ekle
                _context.Add(oneri);
                // Değişiklikleri kaydet
                await _context.SaveChangesAsync();
                // Kullanıcıyı bir teşekkürler sayfasına yönlendir.
                return RedirectToAction(nameof(Tesekkurler));
            }
            // Eğer model geçerli değilse, formu aynı verilerle tekrar göster.
            return View(oneri);
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
            return View();
        }

        // POST: Oneri/Yeni
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yeni(Oneri oneri)
        {
            if (ModelState.IsValid)
            {
                oneri.Tarih = DateTime.Now;
                oneri.Durum = ONERI.Models.OneriDurum.Beklemede; // 0: Beklemede

                _context.Add(oneri);
                await _context.SaveChangesAsync();

                // 4. Büyük Bağlantı: Sistem Bunu Nasıl Kullanacak? (Logic)
                // Kayıt işleminden sonra ilgili bölüm yöneticisinin emailini bul.
                var yonetici = await _context.BolumYoneticileri
                                             .FirstOrDefaultAsync(y => y.BolumAdi.ToLower() == (oneri.Bolum ?? "").ToLower());

                if (yonetici != null)
                {
                    var yoneticiEmail = yonetici.YoneticiEmail;
                    // TODO: İleride bu adrese mail gönderecek fonksiyon yazılacak.
                    // System.Diagnostics.Debug.WriteLine($"Mail gönderilecek: {yoneticiEmail}");
                }
                else
                {
                    // TODO: Bu bölüm için yönetici bulunamazsa ne yapılacağı belirlenecek.
                    // Varsayılan olarak Genel Müdür'e mail atılabilir.
                    // System.Diagnostics.Debug.WriteLine($"Yönetici bulunamadı: {oneri.Bolum}");
                }

                // Pass the saved object (which now has an ID) to the 'Tesekkurler' view.
                return View("Tesekkurler", oneri);
            }
            
            // ModelState geçerli değilse, formu yeniden gösterirken Bölüm listesini tekrar doldur.
            var bolumler = await _context.BolumYoneticileri
                                         .Select(b => b.BolumAdi)
                                         .Distinct()
                                         .ToListAsync();
            ViewBag.Bolumler = new SelectList(bolumler);
            return View(oneri);
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
