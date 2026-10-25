using Microsoft.AspNetCore.Mvc;
using ShopSphere.Services;

namespace ShopSphere.ViewComponents;

// Cart item counter shown in the navbar
public class CartBadgeViewComponent : ViewComponent
{
    private readonly ICartService _cart;

    public CartBadgeViewComponent(ICartService cart) => _cart = cart;

    public IViewComponentResult Invoke() => View(_cart.Count);
}
