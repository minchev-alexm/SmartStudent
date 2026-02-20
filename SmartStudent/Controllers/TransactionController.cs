/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  TransactionController.cs				            Date: 2/20/2026   │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│	           Handles CRUD operations for user transactions				  │
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
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext db;

        public TransactionController(ApplicationDbContext db)
        {
            this.db = db;
        }

        //GET for Index
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get available months/years that contain transactions
            var months = await db.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new MonthYear
                {
                    Month = t.Date.Month,
                    Year = t.Date.Year
                })
                .Distinct()
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToListAsync();

            // Default selection
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

            // Filter transactions for selected month
            var transactions = await db.Transactions
                .Where(t => t.UserId == userId &&
                            t.Date >= startDate &&
                            t.Date < endDate)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            // Monthly totals
            var incomeTotal = transactions
                .Where(t => t.Type == "Income")
                .Sum(t => t.Amount);

            var expenseTotal = transactions
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            // Send data to view
            ViewBag.IncomeTotal = incomeTotal;
            ViewBag.ExpenseTotal = expenseTotal;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.AvailableMonths = months;

            return View(transactions);
        }

        //GET for ViewTransactions
        public async Task<IActionResult> ViewTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        //GET for CreateTransaction
        [HttpGet]
        public IActionResult CreateTransaction()
        {
            var categories = db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories);

            return View();
        }

        //POST for CreateTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(Transaction model)
        {
            //Is form data valid
            if (!ModelState.IsValid)
            {
                var categories = db.Categories.Select(c => c.Name).ToList();
                ViewBag.CategoryList = new SelectList(categories, model.Category);
                return View(model);
            }

            //get user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "User is not logged in.");
                var categories = db.Categories.Select(c => c.Name).ToList();
                ViewBag.CategoryList = new SelectList(categories, model.Category);
                return View(model);
            }
            model.UserId = userId;

            //Upload file
            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.DocumentFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await model.DocumentFile.CopyToAsync(stream);
                    model.DocumentPath = $"/documents/{fileName}";
                }

                catch (Exception ex)
                {
                    ModelState.AddModelError("", "File upload failed.");
                    var categories = db.Categories.Select(c => c.Name).ToList();
                    ViewBag.CategoryList = new SelectList(categories, model.Category);
                    return View(model);
                }
            }

            //Save to DB
            try
            {
                db.Transactions.Add(model);
                await db.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                ModelState.AddModelError("", "Database save failed.");
                var categories = db.Categories.Select(c => c.Name).ToList();
                ViewBag.CategoryList = new SelectList(categories, model.Category);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        //POST for DeleteTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (!string.IsNullOrEmpty(transaction.DocumentPath))
            {
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    transaction.DocumentPath.TrimStart('/'));

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            db.Transactions.Remove(transaction);
            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //GET for EditTransaction
        [HttpGet]
        public async Task<IActionResult> EditTransaction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound();

            var categories = db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories, transaction.Category);
            return View(transaction);
        }

        //POST for EditTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransaction(Transaction model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = await db.Transactions
                .FirstOrDefaultAsync(t => t.Id == model.Id && t.UserId == userId);

            if (transaction == null)
                return NotFound();

            transaction.Date = model.Date;
            transaction.Type = model.Type;
            transaction.Category = model.Category;
            transaction.Amount = model.Amount;

            //File upload
            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "documents");

                Directory.CreateDirectory(uploadsFolder);

                //Replace file
                if (!string.IsNullOrEmpty(transaction.DocumentPath))
                {
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        transaction.DocumentPath.TrimStart('/'));

                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.DocumentFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.DocumentFile.CopyToAsync(stream);

                transaction.DocumentPath = $"/documents/{fileName}";
            }

            //Save to DB
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
