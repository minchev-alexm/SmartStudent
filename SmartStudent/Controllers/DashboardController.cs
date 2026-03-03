/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  DashboaredController.cs				            Date: 2/20/2026   │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│	           Provides financial overview for logged in users and shows	  │
│			                   recent transactions							  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        //GET for Index
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get distinct months/years for which the user has transactions
            var months = await db.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new MonthYear { Month = t.Date.Month, Year = t.Date.Year })
                .Distinct()
                .OrderByDescending(t => t.Year)
                .ThenByDescending(t => t.Month)
                .ToListAsync();

            // Default to latest month/year if none provided
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

            // Define start/end dates for filtering transactions
            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1);

            // Fetch transactions for selected month/year
            var transactions = await db.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToListAsync();

            // Calculate totals
            var incomeTotal = await db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Income" && t.Date.Month == month && t.Date.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expenseTotal = await db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense" && t.Date.Month == month && t.Date.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var balance = incomeTotal - expenseTotal;

            // Budget totals
            var budgets = await db.Budgets.Where(b => b.UserId == userId).ToListAsync();
            var totalPlanned = budgets.Sum(b => b.Planned);
            var totalActual = budgets.Sum(b => b.Actual);

            // Prepare warnings
            var warnings = new List<string>();
            if (balance <= 0)
                warnings.Add("<b>Warning:</b> Your balance is zero or negative!");
            if (totalActual > totalPlanned)
                warnings.Add($"<b>Warning:</b> You have overspent your planned budget by {(totalActual - totalPlanned):C}!");

            var monthOptions = months
                .Select(m => new SelectListItem
                {
                    Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m.Month) + " " + m.Year,
                    Value = m.Month.ToString(),
                    Selected = (m.Month == month && m.Year == year)
                })
                .ToList();

            ViewBag.MonthOptions = monthOptions;
            ViewBag.SelectedYear = year;

            // Pass data to the view
            ViewBag.MonthOptions = monthOptions;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.IncomeTotal = incomeTotal;
            ViewBag.ExpenseTotal = expenseTotal;
            ViewBag.Balance = balance;
            ViewBag.TotalPlannedBudget = totalPlanned;
            ViewBag.TotalActualBudget = totalActual;
            ViewBag.Warnings = warnings;

            return View(transactions);
        }

        //GET for Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Statistics
        public async Task<IActionResult> Statistics(int? startMonth, int? startYear, int? endMonth, int? endYear)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var viewModel = new StatisticsViewModel();

            // Defaults
            var now = DateTime.Now;
            int sMonth = startMonth ?? now.Month;
            int sYear = startYear ?? now.Year;
            int eMonth = endMonth ?? now.Month;
            int eYear = endYear ?? now.Year;

            // Start/end dates
            var startDate = new DateTime(sYear, sMonth, 1);
            var endDate = new DateTime(eYear, eMonth, 1);

            // Swap if start > end
            if (startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;

                TempData["ErrorMessage"] = "Start date was after end date. Dates were swapped automatically.";
            }

            // Include the last month fully
            endDate = endDate.AddMonths(1);

            // Fetch transactions
            var transactions = await db.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            viewModel.RecentTransactions = transactions.Take(10).ToList();
            viewModel.IncomeTotal = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            viewModel.ExpenseTotal = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            // Safe month difference
            int monthDiff = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
            monthDiff = Math.Max(monthDiff, 1);

            // Monthly trend
            var trendMonths = Enumerable.Range(0, monthDiff)
                                        .Select(i => startDate.AddMonths(i))
                                        .ToList();

            viewModel.Months = trendMonths.Select(d => d.ToString("MMM yyyy")).ToList();
            foreach (var d in trendMonths)
            {
                viewModel.MonthlyIncome.Add(transactions
                    .Where(t => t.Type == "Income" && t.Date.Month == d.Month && t.Date.Year == d.Year)
                    .Sum(t => t.Amount));

                viewModel.MonthlyExpense.Add(transactions
                    .Where(t => t.Type == "Expense" && t.Date.Month == d.Month && t.Date.Year == d.Year)
                    .Sum(t => t.Amount));
            }

            // Month dropdown
            ViewBag.MonthList = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
                }).ToList();

            ViewBag.StartMonth = sMonth;
            ViewBag.StartYear = sYear;
            ViewBag.EndMonth = eMonth;
            ViewBag.EndYear = eYear;

            return View(viewModel);
        }
    }
}