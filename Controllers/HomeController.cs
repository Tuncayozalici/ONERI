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
    public async Task<IActionResult> GunlukVeriler(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetGunlukVerilerAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.ProfilLazer)]
    public async Task<IActionResult> ProfilLazerVerileri(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetProfilLazerAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Boyahane)]
    public async Task<IActionResult> BoyahaneDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetBoyahaneAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Pvc)]
    public async Task<IActionResult> PvcDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, string? makine, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetPvcAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, makine, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Cnc)]
    public async Task<IActionResult> CncDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetCncAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Masterwood)]
    public async Task<IActionResult> MasterwoodDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetMasterwoodAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Skipper)]
    public async Task<IActionResult> SkipperDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetSkipperAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.RoverB)]
    public async Task<IActionResult> RoverBDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetRoverBAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Tezgah)]
    public async Task<IActionResult> TezgahDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetTezgahAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.Ebatlama)]
    public async Task<IActionResult> EbatlamaDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, string? makine, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetEbatlamaAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, makine, cancellationToken);
        ApplyViewBagValues(result.ViewBagValues);
        return View(result.Model);
    }

    [Authorize(Policy = Permissions.Dashboards.HataliParca)]
    public async Task<IActionResult> HataliParcaDashboard(DateTime? startDate, DateTime? endDate, DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken)
    {
        var filter = NormalizeDateFilter(startDate, endDate, raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
        var result = await _dashboardQueryService.GetHataliParcaAsync(filter.RaporTarihi, filter.BaslangicTarihi, filter.BitisTarihi, filter.Ay, filter.Yil, cancellationToken);
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

    private static (DateTime? RaporTarihi, DateTime? BaslangicTarihi, DateTime? BitisTarihi, int? Ay, int? Yil) NormalizeDateFilter(
        DateTime? startDate,
        DateTime? endDate,
        DateTime? raporTarihi,
        DateTime? baslangicTarihi,
        DateTime? bitisTarihi,
        int? ay,
        int? yil)
    {
        if (startDate.HasValue || endDate.HasValue)
        {
            var normalized = DateRangeNormalizer.Normalize(startDate ?? endDate ?? DateTime.Today, endDate ?? startDate ?? DateTime.Today);
            return (null, normalized.StartDate, normalized.EndDate, null, null);
        }

        return (raporTarihi, baslangicTarihi, bitisTarihi, ay, yil);
    }
}
