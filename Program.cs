using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Extensions;
using ONERI.Models;
using ONERI.Models.Authorization;
using ONERI.Services.Dashboards;
using ONERI.Services.SuperAdmin;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
var isEfDesignTime = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => string.Equals(a.GetName().Name, "Microsoft.EntityFrameworkCore.Design", StringComparison.Ordinal))
    || string.Equals(Environment.GetEnvironmentVariable("DOTNET_EF_DESIGN_TIME"), "1", StringComparison.Ordinal);
// EPPlus 8 için Lisans Tanımlaması (Ticari Olmayan Kişisel Kullanım)
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Tuncay");

// Add services to the container.
builder.Services.AddDbContext<FabrikaContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddMemoryCache();

// Identity servislerini ve ayarlarını ekle
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        // Güçlü şifre politikası
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Brute-force koruması
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<FabrikaContext>()
    .AddDefaultTokenProviders();

// Yetkilendirme politikaları (rol + izin)
builder.Services.AddAppAuthorizationPolicies();

// Cookie ayarlarını Identity ile entegre et
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MarwoodPortalCookie";
    options.LoginPath = "/Login/Index";
    options.AccessDeniedPath = "/Login/AccessDenied"; // Yetkisiz erişim için yönlendirme (opsiyonel)
});

builder.Services.AddScoped<IDashboardIngestionService, DashboardIngestionService>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
builder.Services.AddScoped<ISuperAdminQueryService, SuperAdminQueryService>();
builder.Services.AddHostedService<DashboardIngestBackgroundService>();


var app = builder.Build();

// Veritabanını başlangıç verileriyle doldur (Roller ve Admin Kullanıcısı)
if (!isEfDesignTime)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<FabrikaContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
            var configuration = services.GetRequiredService<IConfiguration>();
            await DbSeeder.SeedIdentityData(services, configuration);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Veritabanı oluşturulurken veya seed data eklenirken bir hata oluştu.");
        }
    }
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

if (!isEfDesignTime)
{
    app.Run();
}
