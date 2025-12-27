using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;

namespace SmartStudent.Controllers
{
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext db;

        public BudgetController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
