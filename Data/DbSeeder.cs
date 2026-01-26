using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ONERI.Models;

namespace ONERI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedIdentityData(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Roller
            string[] roleNames = { "Yönetici", "Personel" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Yönetici Kullanıcısı
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                var newAdminUser = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@marwood.com",
                    AdSoyad = "Admin"
                };
                // Şifre politikasına uygun (En az 6 karakter, rakam içeren)
                var result = await userManager.CreateAsync(newAdminUser, "123456"); 
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Yönetici");
                }
            }

            // Bölüm Yöneticileri
            var context = services.GetRequiredService<FabrikaContext>();
            if (!await context.BolumYoneticileri.AnyAsync())
            {
                await context.BolumYoneticileri.AddRangeAsync(
                    new BolumYonetici { BolumAdi = "Üretim", YoneticiAdi = "Ahmet Yılmaz", YoneticiEmail = "ahmet.yilmaz@example.com" },
                    new BolumYonetici { BolumAdi = "Kalite Kontrol", YoneticiAdi = "Ayşe Kaya", YoneticiEmail = "ayse.kaya@example.com" },
                    new BolumYonetici { BolumAdi = "Lojistik", YoneticiAdi = "Mehmet Demir", YoneticiEmail = "mehmet.demir@example.com" },
                    new BolumYonetici { BolumAdi = "İnsan Kaynakları", YoneticiAdi = "Fatma Şahin", YoneticiEmail = "fatma.sahin@example.com" },
                    new BolumYonetici { BolumAdi = "Bakım", YoneticiAdi = "Mustafa Arslan", YoneticiEmail = "mustafa.arslan@example.com" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
