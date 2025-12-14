using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;

namespace SmartStudent.Controllers
{
    [Authorize]
    public class ConfigController : Controller
    {
        private readonly ApplicationDbContext db;

        public ConfigController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await db.Categories.ToListAsync();
            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (!ModelState.IsValid) return View(category);
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await db.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category category)
        {
            if (!ModelState.IsValid) return View(category);
            db.Categories.Update(category);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await db.Categories.FindAsync(id);
            if (category != null)
            {
                db.Categories.Remove(category);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        //Settings

        public IActionResult Settings()
        {
            return View();
        }

    }
}