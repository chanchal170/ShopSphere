using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Services;

namespace ShopSphere.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orders;

    public OrdersController(AppDbContext db, IOrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        // a customer may only see their own orders
        if (order.UserId != UserId && !User.IsInRole("Admin")) return Forbid();

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _orders.CancelAsync(id, UserId, User.IsInRole("Admin"));
        TempData[result.Ok ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
