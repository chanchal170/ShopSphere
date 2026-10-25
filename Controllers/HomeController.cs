using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.ViewModels;

namespace ShopSphere.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var active = _db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive);

        var vm = new HomeViewModel
        {
            Featured = await active.Where(p => p.IsFeatured).OrderByDescending(p => p.RatingAverage).Take(8).ToListAsync(),
            NewArrivals = await active.OrderByDescending(p => p.CreatedAt).Take(4).ToListAsync(),
            Deals = await active.Where(p => p.DiscountPercent > 0).OrderByDescending(p => p.DiscountPercent).Take(4).ToListAsync(),
            Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            ProductCount = await _db.Products.CountAsync(p => p.IsActive),
            OrderCount = await _db.Orders.CountAsync(),
            CustomerCount = await _db.UserRoles.CountAsync(ur => _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Customer"))
        };

        vm.CategoryCounts = await _db.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            vm.WishlistIds = (await _db.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync()).ToHashSet();
        }

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View((object)(Activity.Current?.Id ?? HttpContext.TraceIdentifier));

    // shown for 404 / 403 etc. (see UseStatusCodePagesWithReExecute in Program.cs)
    public IActionResult StatusPage(int code)
    {
        Response.StatusCode = code;
        ViewBag.Code = code;
        return View();
    }
}
