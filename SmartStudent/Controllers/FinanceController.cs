using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;

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

        public async Task<IActionResult> ViewTransaction(int id)
        {
            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        [HttpGet]
        public IActionResult CreateTransaction()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction(Transaction model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "documents");

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.DocumentFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.DocumentFile.CopyToAsync(stream);

                model.DocumentPath = $"/documents/{fileName}";
            }

            db.Transactions.Add(model);
            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Transactions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await db.Transactions.FindAsync(id);
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

            return RedirectToAction(nameof(Transactions));
        }


        [HttpGet]
        public async Task<IActionResult> EditTransaction(int id)
        {
            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransaction(Transaction model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var transaction = await db.Transactions.FindAsync(model.Id);
            if (transaction == null)
                return NotFound();

            // Update scalar fields
            transaction.Date = model.Date;
            transaction.Type = model.Type;
            transaction.Category = model.Category;
            transaction.Amount = model.Amount;

            // Handle document upload (overwrite only if new file exists)
            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "documents");

                Directory.CreateDirectory(uploadsFolder);

                // Delete old file if it exists
                if (!string.IsNullOrEmpty(transaction.DocumentPath))
                {
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        transaction.DocumentPath.TrimStart('/'));

                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                // Save new file
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.DocumentFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.DocumentFile.CopyToAsync(stream);

                transaction.DocumentPath = $"/documents/{fileName}";
            }
            // else: no file uploaded → keep existing DocumentPath exactly as-is

            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Transactions));
        }



    }
}
