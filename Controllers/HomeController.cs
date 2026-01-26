using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Models;

namespace ONERI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly FabrikaContext _context;

    public HomeController(ILogger<HomeController> logger, FabrikaContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ToplamSayi = await _context.Oneriler.CountAsync();
        ViewBag.Bekleyen = await _context.Oneriler.CountAsync(o => o.Durum == OneriDurum.Beklemede);
        ViewBag.Onaylanan = await _context.Oneriler.CountAsync(o => o.Durum == OneriDurum.Onaylandi || o.Durum == OneriDurum.KabulEdildi);
        ViewBag.Red = await _context.Oneriler.CountAsync(o => o.Durum == OneriDurum.Reddedildi);

        var sonOneriler = await _context.Oneriler
            .OrderByDescending(o => o.Tarih)
            .Take(5)
            .ToListAsync();

        return View(sonOneriler);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
