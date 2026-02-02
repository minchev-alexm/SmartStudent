using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartStudent.Data;
using SmartStudent.Models;

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

        public IActionResult Index()
        {
            var budgets = _db.Budgets.ToList();

            decimal totalPlanned = budgets.Sum(b => b.Planned);
            decimal totalActual = budgets.Sum(b => b.Actual);
            decimal remaining = totalPlanned - totalActual;
            decimal overspent = totalActual > totalPlanned ? totalActual - totalPlanned : 0;

            ViewBag.TotalPlanned = totalPlanned;
            ViewBag.TotalActual = totalActual;
            ViewBag.Remaining = remaining >= 0 ? remaining : 0;
            ViewBag.Overspent = overspent;

            return View(budgets);
        }

        public IActionResult Add()
        {
            var categories = _db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Budget model)
        {
            if (ModelState.IsValid)
            {
                _db.Budgets.Add(model);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var budget = _db.Budgets.Find(id);
            if (budget == null) return NotFound();

            var categories = _db.Categories.Select(c => c.Name).ToList();
            ViewBag.CategoryList = new SelectList(categories, budget.Category);

            return View(budget);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Budget model)
        {
            if (ModelState.IsValid)
            {
                _db.Budgets.Update(model);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var budget = _db.Budgets.Find(id);
            if (budget == null)
                return NotFound();

            _db.Budgets.Remove(budget);
            _db.SaveChanges();

            return RedirectToAction("Index");
        }

    }
}
