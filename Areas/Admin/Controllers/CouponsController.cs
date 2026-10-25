using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly AppDbContext _db;

    public CouponsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Coupons.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(
        [Bind("Code,Description,DiscountPercent,FlatAmount,MaxDiscount,MinOrderAmount,ExpiresOn,UsageLimit")] Coupon coupon)
    {
        coupon.Code = (coupon.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (coupon.DiscountPercent == 0 && coupon.FlatAmount == 0)
            ModelState.AddModelError("", "Enter a discount percentage or a flat amount.");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["Error"] = string.Join(" ", errors.Where(e => !string.IsNullOrWhiteSpace(e)).DefaultIfEmpty("Invalid coupon."));
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Coupons.AnyAsync(c => c.Code == coupon.Code))
        {
            TempData["Error"] = "A coupon with this code already exists.";
            return RedirectToAction(nameof(Index));
        }

        coupon.IsActive = true;
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Coupon {coupon.Code} created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon == null) return NotFound();

        coupon.IsActive = !coupon.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Coupon {coupon.Code} is now {(coupon.IsActive ? "active" : "disabled")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon == null) return NotFound();

        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Coupon {coupon.Code} deleted.";
        return RedirectToAction(nameof(Index));
    }
}
