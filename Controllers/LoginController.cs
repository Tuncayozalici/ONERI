using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ONERI.Models;
using System.Threading.Tasks;

namespace ONERI.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: /Login
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Login
        [HttpPost]
        public async Task<IActionResult> Index(string KullaniciAdi, string Sifre) // KullaniciAdi e-posta olarak kullanılacak
        {
            if (string.IsNullOrEmpty(KullaniciAdi) || string.IsNullOrEmpty(Sifre))
            {
                ViewBag.Hata = "Kullanıcı adı ve şifre giriniz.";
                return View();
            }

            // Identity'nin PasswordSignInAsync metodunu kullan
            var result = await _signInManager.PasswordSignInAsync(KullaniciAdi, Sifre, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // AdSoyad bilgisini Claim olarak ekleyelim
                var user = await _userManager.FindByNameAsync(KullaniciAdi);
                if (user != null && !string.IsNullOrEmpty(user.AdSoyad))
                {
                    var claims = new List<Claim> { new Claim("AdSoyad", user.AdSoyad) };
                    await _signInManager.UserManager.AddClaimsAsync(user, claims);
                    
                    // Claim'i cooki'ye yansıtmak için kullanıcıyı yeniden sign-in yap
                    await _signInManager.RefreshSignInAsync(user);
                }
                
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                ViewBag.Hata = "Hatalı kullanıcı adı veya şifre.";
                return View();
            }
        }
        
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
