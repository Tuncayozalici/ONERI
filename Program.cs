using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<FabrikaContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// Identity servislerini ve ayarlarını ekle
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        // Şifre gereksinimlerini basitleştir (opsiyonel)
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 4;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<FabrikaContext>();

// Cookie ayarlarını Identity ile entegre et
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MarwoodPortalCookie";
    options.LoginPath = "/Login/Index";
    options.AccessDeniedPath = "/Login/AccessDenied"; // Yetkisiz erişim için yönlendirme (opsiyonel)
});


var app = builder.Build();

// Veritabanını başlangıç verileriyle doldur (Roller ve Admin Kullanıcısı)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedIdentityData(services);
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// Başlangıç verilerini (roller, kullanıcı ve bölümler) oluşturan yardımcı metot
async Task SeedIdentityData(IServiceProvider services)
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
        var result = await userManager.CreateAsync(newAdminUser, "1234");
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
