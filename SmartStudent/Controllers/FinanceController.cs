using Microsoft.AspNetCore.Mvc;

namespace SmartStudent.Controllers
{
    public class FinanceController : Controller
    {
        public IActionResult Budget()
        {
            return View();
        }

        public IActionResult Transactions()
        {
            return View();
        }
    }
}
