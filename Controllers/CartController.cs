using Microsoft.AspNetCore.Mvc;
using ShopSphere.Services;

namespace ShopSphere.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    public async Task<IActionResult> Index() => View(await _cart.GetSummaryAsync());

    // AJAX: re-render only the cart content after a change
    public async Task<IActionResult> Content() => PartialView("_CartContent", await _cart.GetSummaryAsync());

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var result = await _cart.AddAsync(productId, quantity);
        return Json(new { success = result.Ok, message = result.Message, count = _cart.Count });
    }

    [HttpPost]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var result = await _cart.SetQuantityAsync(productId, quantity);
        return Json(new { success = result.Ok, message = result.Message, count = _cart.Count });
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        _cart.Remove(productId);
        return Json(new { success = true, message = "Item removed from cart.", count = _cart.Count });
    }

    [HttpPost]
    public IActionResult Clear()
    {
        _cart.Clear();
        return Json(new { success = true, message = "Cart cleared.", count = 0 });
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string code)
    {
        var result = await _cart.ApplyCouponAsync(code);
        return Json(new { success = result.Ok, message = result.Message, count = _cart.Count });
    }

    [HttpPost]
    public IActionResult RemoveCoupon()
    {
        _cart.RemoveCoupon();
        return Json(new { success = true, message = "Coupon removed.", count = _cart.Count });
    }
}
