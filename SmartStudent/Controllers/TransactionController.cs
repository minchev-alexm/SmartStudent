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

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var transactions = await db.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            ViewBag.IncomeTotal = transactions
                .Where(t => t.Type == "Income")
                .Sum(t => t.Amount);

            ViewBag.ExpenseTotal = transactions
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            return View(transactions);
        }


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

        [HttpGet]
        public IActionResult CreateTransaction()
        {
            var categories = db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(Transaction model)
        {
            Console.WriteLine("CreateTransaction called");
            Console.WriteLine($"Model received: Type={model.Type}, Category={model.Category}, Amount={model.Amount}, Date={model.Date}, DocumentFile={(model.DocumentFile != null ? model.DocumentFile.FileName : "null")}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($" - {state.Key}: {error.ErrorMessage}");
                    }
                }

                var categories = db.Categories.Select(c => c.Name).ToList();
                ViewBag.CategoryList = new SelectList(categories, model.Category);
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"UserId: {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("UserId is null or empty");
                ModelState.AddModelError("", "User is not logged in.");
                var categories = db.Categories.Select(c => c.Name).ToList();
                ViewBag.CategoryList = new SelectList(categories, model.Category);
                return View(model);
            }
            model.UserId = userId;

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
                    Console.WriteLine($"File uploaded successfully: {model.DocumentPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file: {ex.Message}");
                    ModelState.AddModelError("", "File upload failed.");
                    var categories = db.Categories.Select(c => c.Name).ToList();
                    ViewBag.CategoryList = new SelectList(categories, model.Category);
                    return View(model);
                }
            }
            else
            {
                Console.WriteLine("No file uploaded");
            }

            try
            {
                db.Transactions.Add(model);
                Console.WriteLine("Transaction added to context, calling SaveChangesAsync");
                await db.SaveChangesAsync();
                Console.WriteLine($"Transaction saved successfully: Id={model.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving transaction to database: {ex.Message}");
                ModelState.AddModelError("", "Database save failed.");
                var categories = db.Categories.Select(c => c.Name).ToList();
                ViewBag.CategoryList = new SelectList(categories, model.Category);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }


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

            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "documents");

                Directory.CreateDirectory(uploadsFolder);

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

            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



    }
}
