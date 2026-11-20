using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelBookingSystem.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "This email is already registered. Please login instead.");
            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Role = "Customer",
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Registration successful. Please login with your email and password.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Success)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
