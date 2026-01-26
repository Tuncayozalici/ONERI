using Microsoft.AspNetCore.Mvc;

namespace ONERI.Controllers
{
    public class LoginController : Controller
    {
        // GET: /Login
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Login
        [HttpPost]
        public IActionResult Index(string KullaniciAdi, string Sifre)
        {
            // Sunucu tarafı doğrulaması: boş alanları engelle
            if (string.IsNullOrEmpty(KullaniciAdi) || string.IsNullOrEmpty(Sifre))
            {
                ViewBag.Hata = "Kullanıcı adı ve şifre giriniz.";
                return View();
            }

            if (KullaniciAdi == "admin" && Sifre == "1234")
            {
                // Session'a bir değer atayarak kullanıcının giriş yaptığını işaretle
                HttpContext.Session.SetString("GirisYapan", "admin");
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                ViewBag.Hata = "Hatalı kullanıcı adı veya şifre.";
                return View();
            }
        }
    }
}
