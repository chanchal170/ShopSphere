using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly AppDbContext _db;

    public WishlistController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var items = await _db.WishlistItems.AsNoTracking()
            .Include(w => w.Product).ThenInclude(p => p!.Category)
            .Where(w => w.UserId == UserId && w.Product!.IsActive)
            .OrderByDescending(w => w.AddedOn)
            .ToListAsync();
        return View(items);
    }

    // AJAX: add or remove a product from the wishlist
    [HttpPost]
    public async Task<IActionResult> Toggle(int productId)
    {
        var existing = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == UserId && w.ProductId == productId);

        bool added;
        if (existing != null)
        {
            _db.WishlistItems.Remove(existing);
            added = false;
        }
        else
        {
            if (!await _db.Products.AnyAsync(p => p.Id == productId))
                return Json(new { success = false, message = "Product not found." });

            _db.WishlistItems.Add(new WishlistItem { UserId = UserId, ProductId = productId });
            added = true;
        }

        await _db.SaveChangesAsync();
        return Json(new
        {
            success = true,
            added,
            message = added ? "Added to your wishlist." : "Removed from your wishlist."
        });
    }
}
