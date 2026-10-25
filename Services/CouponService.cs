using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Services;

public record CouponResult(bool IsValid, string Message, decimal Discount, string? Code);

public interface ICouponService
{
    Task<CouponResult> ValidateAsync(string code, decimal subtotal);
}

public class CouponService : ICouponService
{
    private readonly AppDbContext _db;

    public CouponService(AppDbContext db) => _db = db;

    public async Task<CouponResult> ValidateAsync(string code, decimal subtotal)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length == 0)
            return new CouponResult(false, "Please enter a coupon code.", 0, null);

        var coupon = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);

        if (coupon == null || !coupon.IsActive)
            return new CouponResult(false, "This coupon code is not valid.", 0, null);

        if (coupon.ExpiresOn.HasValue && coupon.ExpiresOn.Value.Date < DateTime.UtcNow.Date)
            return new CouponResult(false, "This coupon has expired.", 0, null);

        if (coupon.UsageLimit > 0 && coupon.TimesUsed >= coupon.UsageLimit)
            return new CouponResult(false, "This coupon has reached its usage limit.", 0, null);

        if (subtotal < coupon.MinOrderAmount)
            return new CouponResult(false, $"Add items worth ₹{coupon.MinOrderAmount:N0} or more to use this coupon.", 0, null);

        decimal discount = coupon.DiscountPercent > 0
            ? Math.Round(subtotal * coupon.DiscountPercent / 100m, 2)
            : coupon.FlatAmount;

        if (coupon.DiscountPercent > 0 && coupon.MaxDiscount > 0 && discount > coupon.MaxDiscount)
            discount = coupon.MaxDiscount;

        if (discount > subtotal) discount = subtotal;

        return new CouponResult(true, $"Coupon {coupon.Code} applied!", discount, coupon.Code);
    }
}
