using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Helpers;
using ShopSphere.ViewModels;

namespace ShopSphere.Services;

public interface ICartService
{
    int Count { get; }
    Task<CartSummary> GetSummaryAsync();
    Task<(bool Ok, string Message)> AddAsync(int productId, int quantity);
    Task<(bool Ok, string Message)> SetQuantityAsync(int productId, int quantity);
    void Remove(int productId);
    void Clear();
    Task<(bool Ok, string Message)> ApplyCouponAsync(string code);
    void RemoveCoupon();
}

// The cart lives in the user's session: { productId -> quantity }
public class CartService : ICartService
{
    private const string CartKey = "cart";
    private const string CouponKey = "coupon";

    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;
    private readonly ICouponService _coupons;

    public CartService(IHttpContextAccessor http, AppDbContext db, ICouponService coupons)
    {
        _http = http;
        _db = db;
        _coupons = coupons;
    }

    private ISession Session => _http.HttpContext!.Session;

    private Dictionary<int, int> Read() => Session.GetJson<Dictionary<int, int>>(CartKey) ?? new();
    private void Write(Dictionary<int, int> cart) => Session.SetJson(CartKey, cart);

    public int Count => Read().Values.Sum();

    public async Task<(bool Ok, string Message)> AddAsync(int productId, int quantity)
    {
        if (quantity < 1) quantity = 1;

        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null) return (false, "Product not found.");
        if (product.Stock <= 0) return (false, "Sorry, this product is out of stock.");

        var cart = Read();
        cart.TryGetValue(productId, out var current);
        var wanted = current + quantity;

        if (wanted > product.Stock)
            return (false, current > 0
                ? $"Only {product.Stock} in stock (you already have {current} in your cart)."
                : $"Only {product.Stock} left in stock.");

        if (wanted > ShopSettings.MaxQuantityPerItem)
            return (false, $"You can buy at most {ShopSettings.MaxQuantityPerItem} of one product.");

        cart[productId] = wanted;
        Write(cart);
        return (true, $"'{product.Name}' added to your cart.");
    }

    public async Task<(bool Ok, string Message)> SetQuantityAsync(int productId, int quantity)
    {
        var cart = Read();
        if (!cart.ContainsKey(productId)) return (false, "Item is not in your cart.");

        if (quantity <= 0)
        {
            Remove(productId);
            return (true, "Item removed.");
        }

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null) { Remove(productId); return (false, "This product is no longer available."); }

        if (quantity > product.Stock) return (false, $"Only {product.Stock} in stock.");
        if (quantity > ShopSettings.MaxQuantityPerItem)
            return (false, $"You can buy at most {ShopSettings.MaxQuantityPerItem} of one product.");

        cart[productId] = quantity;
        Write(cart);
        return (true, "Cart updated.");
    }

    public void Remove(int productId)
    {
        var cart = Read();
        if (cart.Remove(productId)) Write(cart);
    }

    public void Clear()
    {
        Session.Remove(CartKey);
        Session.Remove(CouponKey);
    }

    public async Task<(bool Ok, string Message)> ApplyCouponAsync(string code)
    {
        var summary = await GetSummaryAsync();
        if (!summary.Lines.Any()) return (false, "Your cart is empty.");

        var result = await _coupons.ValidateAsync(code, summary.Subtotal);
        if (!result.IsValid) return (false, result.Message);

        Session.SetString(CouponKey, result.Code!);
        return (true, result.Message);
    }

    public void RemoveCoupon() => Session.Remove(CouponKey);

    public async Task<CartSummary> GetSummaryAsync()
    {
        var cart = Read();
        var ids = cart.Keys.ToList();

        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        var summary = new CartSummary();
        var changed = false;

        foreach (var item in cart)
        {
            var p = products.FirstOrDefault(x => x.Id == item.Key);
            if (p == null || p.Stock <= 0) { changed = true; continue; }   // product removed or sold out

            var qty = Math.Min(item.Value, Math.Min(p.Stock, ShopSettings.MaxQuantityPerItem));
            if (qty != item.Value) changed = true;

            summary.Lines.Add(new CartLine
            {
                ProductId = p.Id,
                Name = p.Name,
                UnitPrice = p.FinalPrice,
                OriginalPrice = p.Price,
                Quantity = qty,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                Icon = p.Category?.Icon ?? "box-seam"
            });
        }

        // keep the session in sync if something changed (price, stock, removed product)
        if (changed) Write(summary.Lines.ToDictionary(l => l.ProductId, l => l.Quantity));

        summary.Subtotal = summary.Lines.Sum(l => l.LineTotal);

        var code = Session.GetString(CouponKey);
        if (!string.IsNullOrEmpty(code) && summary.Lines.Any())
        {
            var result = await _coupons.ValidateAsync(code, summary.Subtotal);
            if (result.IsValid)
            {
                summary.CouponCode = result.Code;
                summary.Discount = result.Discount;
            }
            else
            {
                Session.Remove(CouponKey);   // coupon is no longer valid for this cart
            }
        }

        var afterDiscount = summary.Subtotal - summary.Discount;
        summary.Shipping = summary.Lines.Count == 0 ? 0
            : afterDiscount >= ShopSettings.FreeShippingThreshold ? 0 : ShopSettings.ShippingFee;
        summary.Total = afterDiscount + summary.Shipping;

        return summary;
    }
}
