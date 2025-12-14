using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;

namespace SmartStudent.Controllers
{
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext db;

        public FinanceController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public IActionResult Budget()
        {
            return View();
        }

        public async Task<IActionResult> Transactions()
        {
            var transactions = await db.Transactions.ToListAsync();
            return View(transactions);
        }
    }
}
