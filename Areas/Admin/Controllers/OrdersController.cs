using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orders;

    public OrdersController(AppDbContext db, IOrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    public async Task<IActionResult> Index(string? q, OrderStatus? status)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(term) ||
                                     o.ShippingName.ToLower().Contains(term) ||
                                     o.Phone.Contains(term));
        }

        ViewBag.Query = q;
        ViewBag.Status = status;
        return View(await query.OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var result = await _orders.UpdateStatusAsync(id, status);
        TempData[result.Ok ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
