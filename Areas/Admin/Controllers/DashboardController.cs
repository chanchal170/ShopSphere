using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // money values are summed in memory (SQLite is not good at decimal maths)
        var orders = await _db.Orders.AsNoTracking()
            .Select(o => new { o.CreatedAt, o.Total, o.Status })
            .ToListAsync();

        var paid = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var today = DateTime.UtcNow.Date;

        var vm = new AdminDashboardViewModel
        {
            TotalRevenue = paid.Sum(o => o.Total),
            RevenueToday = paid.Where(o => o.CreatedAt.Date == today).Sum(o => o.Total),
            TotalOrders = orders.Count,
            OrdersToday = orders.Count(o => o.CreatedAt.Date == today),
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            TotalProducts = await _db.Products.CountAsync(),
            TotalCustomers = await _db.UserRoles.CountAsync(ur => _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Customer"))
        };

        // revenue for the last 7 days
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            vm.DayLabels.Add(day.ToString("ddd dd"));
            vm.DayRevenue.Add(paid.Where(o => o.CreatedAt.Date == day).Sum(o => o.Total));
        }

        // orders by status (doughnut chart)
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            vm.StatusLabels.Add(status.ToString());
            vm.StatusCounts.Add(orders.Count(o => o.Status == status));
        }

        var top = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order!.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.ProductName)
            .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(5)
            .ToListAsync();
        vm.TopProducts = top.Select(t => (t.Name, t.Qty)).ToList();

        vm.LowStock = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= 5)
            .OrderBy(p => p.Stock)
            .Take(6)
            .ToListAsync();

        vm.RecentOrders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .ToListAsync();

        return View(vm);
    }
}
