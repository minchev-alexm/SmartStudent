/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  AccountController.cs				            Date: 2/20/2026       │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│	  This controller handles user authentication and account management.	  │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;
using System.Security.Claims;

namespace SmartStudent.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext db;

        public AccountController(ApplicationDbContext db)
        {
            this.db = db;
        }

        //GET for login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //POST for login page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //DB lookup
            User? user = null;

            foreach (User u in db.Users)
            {
                if (u.Email == model.Email)
                {
                    user = u;
                    break;
                }
            }

            //Wrong credentials
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            //Password check
            bool passwordOk = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!passwordOk)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            //Create claims
            List<Claim> claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, user.Name));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            AuthenticationProperties props = new AuthenticationProperties();

            //Sign in
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            //Redirect
            return RedirectToAction("Index", "Dashboard");
        }

        //GET for Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        //POST for Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // POST for Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            bool emailExists = await db.Users.AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(model);
            }

            // Hash password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            User user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = passwordHash
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            List<Claim> claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };

            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
