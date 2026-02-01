using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace SmartStudent.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext db;

        public DashboardController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var transactions = await db.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToListAsync();

            var incomeTotal = await db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Income")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expenseTotal = await db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var balance = incomeTotal - expenseTotal;

            ViewBag.IncomeTotal = incomeTotal;
            ViewBag.ExpenseTotal = expenseTotal;
            ViewBag.Balance = balance;

            // Warnings
            var warnings = new List<string>();
            if (balance <= 0)
                warnings.Add("<b>Warning:</b> Your balance is zero or negative!");

            ViewBag.Warnings = warnings;

            return View(transactions);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
