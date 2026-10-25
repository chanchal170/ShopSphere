using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly AppDbContext _db;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, AppDbContext db)
    {
        _users = users;
        _signIn = signIn;
        _db = db;
    }

    // ---------------- LOGIN ----------------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            TempData["Success"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            if (await _users.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", result.IsLockedOut
            ? "Too many failed attempts. Your account is locked for 5 minutes."
            : "Invalid email or password.");
        return View(model);
    }

    // ---------------- REGISTER ----------------
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                // "Username 'x' is already taken" is confusing because we use email as username
                var text = error.Code is "DuplicateUserName" or "DuplicateEmail"
                    ? "This email is already registered."
                    : error.Description;
                ModelState.AddModelError("", text);
            }
            return View(model);
        }

        await _users.AddToRoleAsync(user, "Customer");
        await _signIn.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "Your account has been created. Happy shopping!";
        return RedirectToAction("Index", "Home");
    }

    // ---------------- LOGOUT ----------------
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    // ---------------- PROFILE ----------------
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        return View(await BuildProfile(user));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            var vm = await BuildProfile(user);
            vm.FullName = model.FullName;
            vm.Phone = model.Phone;
            vm.Address = model.Address;
            vm.City = model.City;
            return View(vm);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        user.Address = model.Address?.Trim();
        user.City = model.City?.Trim();
        await _users.UpdateAsync(user);

        await _signIn.RefreshSignInAsync(user);   // refresh the cookie so the new name shows immediately
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------------- CHANGE PASSWORD ----------------
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }

        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    public IActionResult AccessDenied() => View();

    private async Task<ProfileViewModel> BuildProfile(ApplicationUser user) => new()
    {
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.PhoneNumber,
        Address = user.Address,
        City = user.City,
        MemberSince = user.CreatedAt,
        OrderCount = await _db.Orders.CountAsync(o => o.UserId == user.Id)
    };
}
