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
        options.Password.RequireDigit = true; // Rakam zorunlu olsun
        options.Password.RequiredLength = 6; // En az 6 karakter
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
    await DbSeeder.SeedIdentityData(services);
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
