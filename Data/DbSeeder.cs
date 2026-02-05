using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ONERI.Models;
using ONERI.Models.Authorization;
using System.Collections.Generic;
using System.Security.Claims;

namespace ONERI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedIdentityData(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Roller
            string[] roleNames = { Permissions.SuperAdminRole, "Yönetici", "Personel" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Yönetici Kullanıcısı (Super Admin)
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                var newAdminUser = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@marwood.com",
                    AdSoyad = "Admin"
                };
                // Şifre politikasına uygun, daha güvenli bir başlangıç şifresi
                var result = await userManager.CreateAsync(newAdminUser, "Qwerty.1234"); 
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, Permissions.SuperAdminRole);
                    await userManager.AddToRoleAsync(newAdminUser, "Yönetici");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, Permissions.SuperAdminRole))
                {
                    await userManager.AddToRoleAsync(adminUser, Permissions.SuperAdminRole);
                }
            }

            // Varsayılan rol izinleri
            var rolePermissionMap = new Dictionary<string, string[]>
            {
                ["Yönetici"] = new[]
                {
                    Permissions.OneriAdmin.Access,
                    Permissions.OneriAdmin.Detail,
                    Permissions.OneriAdmin.Approve,
                    Permissions.OneriAdmin.Reject,
                    Permissions.OneriAdmin.Delete,
                    Permissions.Oneri.Evaluate,
                    Permissions.BolumYoneticileri.View,
                    Permissions.BolumYoneticileri.Create,
                    Permissions.BolumYoneticileri.Delete,
                    Permissions.VeriYukle.Create
                },
                ["Personel"] = new[]
                {
                    Permissions.FikirAtolyesi.View,
                    Permissions.Oneri.Create,
                    Permissions.Oneri.Query
                }
            };

            foreach (var entry in rolePermissionMap)
            {
                var role = await roleManager.FindByNameAsync(entry.Key);
                if (role == null)
                {
                    continue;
                }

                var existingClaims = await roleManager.GetClaimsAsync(role);
                foreach (var permission in entry.Value)
                {
                    var alreadyHas = existingClaims.Any(c => c.Type == Permissions.ClaimType && c.Value == permission);
                    if (!alreadyHas)
                    {
                        await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
                    }
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
