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
        public async Task<IActionResult> CreateTransaction(Transaction transaction, IFormFile DocumentPath)
        {
            if (ModelState.IsValid)
            {
                if (DocumentPath != null && DocumentPath.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    //Generate unique file name
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(DocumentPath.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    //Save to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await DocumentPath.CopyToAsync(stream);
                    }

                    //Store relative path
                    transaction.DocumentPath = $"/documents/{uniqueFileName}";
                }

                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Transactions));
            }

            return View(transaction);
        }


    }
}
