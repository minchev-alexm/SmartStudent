using Microsoft.AspNetCore.Mvc;

namespace SmartStudent.Controllers
{
    public class ConfigController : Controller
    {
        public IActionResult Categories()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}
