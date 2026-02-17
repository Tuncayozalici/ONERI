using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONERI.Models;
using ONERI.Models.Authorization;
using ONERI.Services.Dashboards;

namespace ONERI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDashboardQueryService _dashboardQueryService;

    public HomeController(ILogger<HomeController> logger, IDashboardQueryService dashboardQueryService)
    {
        _logger = logger;
        _dashboardQueryService = dashboardQueryService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize(Policy = Permissions.Dashboards.GunlukVeriler)]
    public async Task<IActionResult> GunlukVeriler(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetGunlukVerilerAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.ProfilLazer)]
    public async Task<IActionResult> ProfilLazerVerileri(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetProfilLazerAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Boyahane)]
    public async Task<IActionResult> BoyahaneDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetBoyahaneAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Pvc)]
    public async Task<IActionResult> PvcDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetPvcAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Cnc)]
    public IActionResult CncDashboard()
    {
        return View();
    }

    [Authorize(Policy = Permissions.Dashboards.Masterwood)]
    public async Task<IActionResult> MasterwoodDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetMasterwoodAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Skipper)]
    public async Task<IActionResult> SkipperDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetSkipperAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.RoverB)]
    public async Task<IActionResult> RoverBDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetRoverBAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Tezgah)]
    public async Task<IActionResult> TezgahDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetTezgahAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Ebatlama)]
    public async Task<IActionResult> EbatlamaDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetEbatlamaAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.HataliParca)]
    public async Task<IActionResult> HataliParcaDashboard(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var result = await _dashboardQueryService.GetHataliParcaAsync(raporTarihi, ay, yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError("Uygulama hatası yakalandı. RequestId: {RequestId}", requestId);
        return View(new ErrorViewModel { RequestId = requestId });
    }

    private void ApplyViewBagValues(Dictionary<string, object?> values)
    {
        foreach (var kvp in values)
        {
            ViewData[kvp.Key] = kvp.Value;
        }
    }
}
