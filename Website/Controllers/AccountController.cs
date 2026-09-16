using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
			{
				var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username);
				if (admin != null)
				{
					var thispassword = BCrypt.Net.BCrypt.Verify(model.Password, admin.Password);
                    if (thispassword)
					{
                        //administrator P@ssw0rd
                        //dreman dreman123
						// Update LastLogin and Audit Status
						admin.LastLogin = DateTime.UtcNow;
						_context.Update(admin);
						await _context.SaveChangesAsync();

						var claims = new List<Claim>
					    {
						    new Claim(ClaimTypes.Name, admin.Username),
						    new Claim(ClaimTypes.Role, admin.Role.ToString()) // dynamic role
                        };

						var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
						var authProperties = new AuthenticationProperties
						{
							// Enable sliding expiration and use the settings defined in Program.cs
							AllowRefresh = true,
							IsPersistent = true
						};

						await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

						return RedirectToAction("Index", "Home");
					}
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

                var admin = _context.Admins.FirstOrDefault(a => a.Username == username);

                if (admin != null && BCrypt.Net.BCrypt.Verify(model.OldPassword, admin.Password))
                {
                    admin.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    admin.UpdateBy = User.Identity?.Name ?? "System";
                    admin.UpdateDate = DateTime.UtcNow;
                    
                    _context.Update(admin);
                    await _context.SaveChangesAsync();


                    TempData["Message"] = "Password changed successfully!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Incorrect current password.");
            }
            return View(model);
        }
    }
}
