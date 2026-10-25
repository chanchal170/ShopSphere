using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;
using ShopSphere.ViewModels;

namespace ShopSphere.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public CheckoutController(ICartService cart, IOrderService orders, UserManager<ApplicationUser> users, AppDbContext db)
    {
        _cart = cart;
        _orders = orders;
        _users = users;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await _cart.GetSummaryAsync();
        if (!cart.Lines.Any())
        {
            TempData["Error"] = "Your cart is empty. Add some products first.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _users.GetUserAsync(User);
        return View(new CheckoutViewModel
        {
            ShippingName = user?.FullName ?? "",
            Phone = user?.PhoneNumber ?? "",
            Address = user?.Address ?? "",
            City = user?.City ?? "",
            Cart = cart
        });
    }

    [HttpPost]
    public async Task<IActionResult> Place(CheckoutViewModel model)
    {
        model.Cart = await _cart.GetSummaryAsync();
        if (!model.Cart.Lines.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid) return View("Index", model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (order, error) = await _orders.PlaceOrderAsync(userId, model);

        if (order == null)
        {
            ModelState.AddModelError("", error ?? "Could not place the order.");
            model.Cart = await _cart.GetSummaryAsync();
            return View("Index", model);
        }

        return RedirectToAction(nameof(Success), new { orderNumber = order.OrderNumber });
    }

    [HttpGet]
    public async Task<IActionResult> Success(string orderNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == userId);

        return order == null ? NotFound() : View(order);
    }
}
