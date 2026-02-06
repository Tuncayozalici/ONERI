using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ONERI.Models;
using ONERI.Models.Authorization;
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
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string KullaniciAdi, string Sifre, string? returnUrl = null) // KullaniciAdi e-posta olarak kullanılacak
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(KullaniciAdi) || string.IsNullOrEmpty(Sifre))
            {
                ViewBag.Hata = "Kullanıcı adı ve şifre giriniz.";
                return View();
            }

            // Identity'nin PasswordSignInAsync metodunu kullan
            var result = await _signInManager.PasswordSignInAsync(KullaniciAdi, Sifre, isPersistent: true, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // AdSoyad bilgisini Claim olarak ekleyelim
                var user = await _userManager.FindByNameAsync(KullaniciAdi);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(user.AdSoyad))
                    {
                        var existingClaims = await _userManager.GetClaimsAsync(user);
                        var existingNameClaim = existingClaims.FirstOrDefault(c => c.Type == "AdSoyad");
                        var updatedNameClaim = new Claim("AdSoyad", user.AdSoyad);

                        if (existingNameClaim == null)
                        {
                            await _userManager.AddClaimAsync(user, updatedNameClaim);
                        }
                        else if (existingNameClaim.Value != user.AdSoyad)
                        {
                            await _userManager.ReplaceClaimAsync(user, existingNameClaim, updatedNameClaim);
                        }
                    }

                    // İlk sign-in akışında claim değişikliklerini cookie'ye yansıt.
                    await _signInManager.SignInAsync(user, isPersistent: true);

                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    if (await _userManager.IsInRoleAsync(user, Permissions.SuperAdminRole) ||
                        await _userManager.IsInRoleAsync(user, "Yönetici"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ViewBag.Hata = "Hesap geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.";
                return View();
            }

            ViewBag.Hata = "Hatalı kullanıcı adı veya şifre.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
