using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Services;

public interface IOrderService
{
    Task<(Order? Order, string? Error)> PlaceOrderAsync(string userId, CheckoutViewModel model);
    Task<(bool Ok, string Message)> CancelAsync(int orderId, string userId, bool isAdmin);
    Task<(bool Ok, string Message)> UpdateStatusAsync(int orderId, OrderStatus status);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ICartService cart, ILogger<OrderService> logger)
    {
        _db = db;
        _cart = cart;
        _logger = logger;
    }

    public async Task<(Order? Order, string? Error)> PlaceOrderAsync(string userId, CheckoutViewModel model)
    {
        var cart = await _cart.GetSummaryAsync();
        if (!cart.Lines.Any()) return (null, "Your cart is empty.");

        // Everything below happens in one transaction: either the whole order is saved or nothing is.
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ids = cart.Lines.Select(l => l.ProductId).ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            // final stock check
            foreach (var line in cart.Lines)
            {
                var p = products.FirstOrDefault(x => x.Id == line.ProductId);
                if (p == null || !p.IsActive || p.Stock < line.Quantity)
                    return (null, $"Sorry, '{line.Name}' is no longer available in the quantity you selected. Please review your cart.");
            }

            var order = new Order
            {
                UserId = userId,
                OrderNumber = await NewOrderNumberAsync(),
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                ShippingName = model.ShippingName.Trim(),
                Phone = model.Phone.Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                PaymentMethod = model.PaymentMethod,
                Subtotal = cart.Subtotal,
                Discount = cart.Discount,
                Shipping = cart.Shipping,
                Total = cart.Total,
                CouponCode = cart.CouponCode
            };

            foreach (var line in cart.Lines)
            {
                var p = products.First(x => x.Id == line.ProductId);
                p.Stock -= line.Quantity;
                order.Items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = p.FinalPrice,
                    Quantity = line.Quantity
                });
            }

            if (!string.IsNullOrEmpty(cart.CouponCode))
            {
                var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == cart.CouponCode);
                if (coupon != null) coupon.TimesUsed++;
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            _cart.Clear();
            _logger.LogInformation("Order {OrderNumber} placed by user {UserId} for {Total}", order.OrderNumber, userId, order.Total);
            return (order, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for user {UserId}", userId);
            return (null, "Something went wrong while placing your order. Please try again.");
        }
    }

    public async Task<(bool Ok, string Message)> CancelAsync(int orderId, string userId, bool isAdmin)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return (false, "Order not found.");
        if (order.UserId != userId && !isAdmin) return (false, "You cannot cancel this order.");
        if (!order.CanCancel) return (false, "This order can no longer be cancelled.");

        await RestockAsync(order);
        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} cancelled", order.OrderNumber);
        return (true, $"Order {order.OrderNumber} has been cancelled.");
    }

    public async Task<(bool Ok, string Message)> UpdateStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return (false, "Order not found.");
        if (order.Status == status) return (true, "Status is already " + status + ".");
        if (order.Status == OrderStatus.Cancelled) return (false, "A cancelled order cannot be changed.");
        if (order.Status == OrderStatus.Delivered && status == OrderStatus.Cancelled)
            return (false, "A delivered order cannot be cancelled.");

        if (status == OrderStatus.Cancelled) await RestockAsync(order);

        order.Status = status;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} status changed to {Status}", order.OrderNumber, status);
        return (true, $"Order {order.OrderNumber} marked as {status}.");
    }

    // give the stock back when an order is cancelled
    private async Task RestockAsync(Order order)
    {
        var ids = order.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        foreach (var item in order.Items)
        {
            var p = products.FirstOrDefault(x => x.Id == item.ProductId);
            if (p != null) p.Stock += item.Quantity;
        }
    }

    private async Task<string> NewOrderNumberAsync()
    {
        while (true)
        {
            var number = $"SS{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(1000, 9999)}";
            if (!await _db.Orders.AnyAsync(o => o.OrderNumber == number)) return number;
        }
    }
}
