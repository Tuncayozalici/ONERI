using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Models;
using Microsoft.AspNetCore.Authorization;
using ONERI.Models.Authorization;

namespace ONERI.Controllers;

public class FikirAtolyesiController : Controller
{
    private readonly ILogger<FikirAtolyesiController> _logger;
    private readonly FabrikaContext _context;

    public FikirAtolyesiController(ILogger<FikirAtolyesiController> logger, FabrikaContext context)
    {
        _logger = logger;
        _context = context;
    }

    [Authorize(Policy = Permissions.FikirAtolyesi.View)]
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
