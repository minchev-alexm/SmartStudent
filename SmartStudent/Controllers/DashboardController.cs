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

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var months = await db.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new MonthYear { Month = t.Date.Month, Year = t.Date.Year })
                .Distinct()
                .OrderByDescending(t => t.Year)
                .ThenByDescending(t => t.Month)
                .ToListAsync();

            if (!month.HasValue || !year.HasValue)
            {
                var latest = months.FirstOrDefault();
                if (latest != null)
                {
                    month = latest.Month;
                    year = latest.Year;
                }
                else
                {
                    month = DateTime.Now.Month;
                    year = DateTime.Now.Year;
                }

            }
            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1);

            var transactions = await db.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToListAsync();


            var incomeTotal = await db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Income" && t.Date.Month == month && t.Date.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expenseTotal = await db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense" && t.Date.Month == month && t.Date.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var balance = incomeTotal - expenseTotal;
            var budgets = await db.Budgets.Where(b => b.UserId == userId).ToListAsync();
            var totalPlanned = budgets.Sum(b => b.Planned);
            var totalActual = budgets.Sum(b => b.Actual);

            ViewBag.TotalPlannedBudget = totalPlanned;
            ViewBag.TotalActualBudget = totalActual;

            ViewBag.IncomeTotal = incomeTotal;
            ViewBag.ExpenseTotal = expenseTotal;
            ViewBag.Balance = balance;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.AvailableMonths = months;

            var warnings = new List<string>();
            if (balance <= 0)
                warnings.Add("<b>Warning:</b> Your balance is zero or negative!");

            if (totalActual > totalPlanned)
            {
                var overspentAmount = totalActual - totalPlanned;
                warnings.Add($"<b>Warning:</b> You have overspent your planned budget by {overspentAmount:C}!");
            }

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