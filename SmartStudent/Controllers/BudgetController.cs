/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  BudgetController.cs				            Date: 2/20/2026       │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│	               Handles CRUD operations for user budget			          │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;
using System.Security.Claims;

namespace SmartStudent.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BudgetController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET for Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch budgets for the current user
            var budgets = await _db.Budgets
                .Where(b => b.UserId == userId)
                .ToListAsync();

            // Summary calculations
            decimal totalPlanned = budgets.Sum(b => b.Planned);
            decimal totalActual = budgets.Sum(b => b.Actual);
            decimal remaining = totalPlanned - totalActual;
            decimal overspent = totalActual > totalPlanned ? totalActual - totalPlanned : 0;

            // Fetch transactions for this user to integrate income/expense totals
            var incomeTotal = await _db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Income")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expenseTotal = await _db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense")
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            ViewBag.TotalPlanned = totalPlanned;
            ViewBag.TotalActual = totalActual;
            ViewBag.Remaining = remaining >= 0 ? remaining : 0;
            ViewBag.Overspent = overspent;

            ViewBag.IncomeTotal = incomeTotal;
            ViewBag.ExpenseTotal = expenseTotal;
            ViewBag.Balance = incomeTotal - expenseTotal;

            return View(budgets);
        }

        //GET for Add
        public IActionResult Add()
        {
            var categories = _db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories);
            return View();
        }

        //POST for Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Budget model)
        {
            if (ModelState.IsValid)
            {
                model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _db.Budgets.Add(model);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        //GET for Edit
        public IActionResult Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var budget = _db.Budgets.FirstOrDefault(b => b.Id == id && b.UserId == userId);
            if (budget == null) return NotFound();

            var categories = _db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories, budget.Category);

            return View(budget);
        }

        //POST for Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Budget model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var budget = _db.Budgets.FirstOrDefault(b => b.Id == model.Id && b.UserId == userId);
                if (budget == null) return NotFound();

                budget.Category = model.Category;
                budget.Planned = model.Planned;
                budget.Actual = model.Actual;

                _db.Budgets.Update(budget);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        //POST for Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var budget = _db.Budgets.FirstOrDefault(b => b.Id == id && b.UserId == userId);
            if (budget == null) return NotFound();

            _db.Budgets.Remove(budget);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
