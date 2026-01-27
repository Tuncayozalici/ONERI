using Microsoft.AspNetCore.Mvc;

namespace ONERI.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
