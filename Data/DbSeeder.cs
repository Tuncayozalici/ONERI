using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ONERI.Models;
using ONERI.Models.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ONERI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedIdentityData(IServiceProvider services, IConfiguration configuration)
        {
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            // Roller
            var roleNames = new[] { Permissions.SuperAdminRole }
                .Concat(RolePermissionMatrix.GetRoleNames())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // İlk kurulum Super Admin kullanıcısı
            var bootstrapUserName = configuration["BootstrapAdmin:UserName"] ?? "admin";
            var bootstrapEmail = configuration["BootstrapAdmin:Email"] ?? "admin@marwood.com";
            var bootstrapFullName = configuration["BootstrapAdmin:FullName"] ?? "Admin";
            var bootstrapPassword = configuration["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                bootstrapPassword = Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD");
            }
            var adminUser = await userManager.FindByNameAsync(bootstrapUserName);
            if (adminUser == null)
            {
                if (string.IsNullOrWhiteSpace(bootstrapPassword))
                {
                    logger.LogWarning("Super Admin oluşturulmadı. İlk kurulum için 'BootstrapAdmin:Password' secret'ı tanımlayın.");
                }
                else
                {
                    var newAdminUser = new AppUser
                    {
                        UserName = bootstrapUserName,
                        Email = bootstrapEmail,
                        AdSoyad = bootstrapFullName
                    };

                    var result = await userManager.CreateAsync(newAdminUser, bootstrapPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdminUser, Permissions.SuperAdminRole);
                        await userManager.AddToRoleAsync(newAdminUser, "Yönetici");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Super Admin oluşturulamadı: {Errors}", errors);
                    }
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
            foreach (var entry in RolePermissionMatrix.DefaultRolePermissions)
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
