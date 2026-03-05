using System.Security.Claims;
using BizSecureDemo_22180084.Data;
using BizSecureDemo_22180084.Models;
using BizSecureDemo_22180084.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BizSecureDemo_22180084.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher;

    public AccountController(AppDbContext db, PasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // -------- REGISTER --------
    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError("", "Този email вече е регистриран.");
            return View(vm);
        }

        var user = new AppUser { Email = email };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);

        // init security fields
        user.FailedLogins = 0;
        user.LockoutUntilUtc = null;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Login));
    }

    // -------- LOGIN --------
    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Не издаваме дали email-ът съществува
        if (user == null)
        {
            ModelState.AddModelError("", "Грешен email или парола.");
            return View(vm);
        }

        var now = DateTime.UtcNow;

        // Проверка за lockout
        if (user.LockoutUntilUtc.HasValue && user.LockoutUntilUtc.Value > now)
        {
            var secondsLeft = (int)Math.Ceiling((user.LockoutUntilUtc.Value - now).TotalSeconds);
            if (secondsLeft < 1) secondsLeft = 1;

            ModelState.AddModelError("", $"Акаунтът е временно заключен. Опитай след {secondsLeft} сек.");
            return View(vm);
        }

        // Ако lockout е изтекъл - чистим полетата
        if (user.LockoutUntilUtc.HasValue && user.LockoutUntilUtc.Value <= now)
        {
            user.LockoutUntilUtc = null;
            user.FailedLogins = 0;
            await _db.SaveChangesAsync();
        }

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password);

        // Грешна парола
        if (verify == PasswordVerificationResult.Failed)
        {
            user.FailedLogins++;

            if (user.FailedLogins >= 5)
            {
                user.FailedLogins = 0;
                user.LockoutUntilUtc = now.AddMinutes(5);
            }

            await _db.SaveChangesAsync();

            ModelState.AddModelError("", "Грешен email или парола.");
            return View(vm);
        }

        // Успешен вход -> reset на брояча/lockout
        user.FailedLogins = 0;
        user.LockoutUntilUtc = null;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    // -------- LOGOUT --------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}