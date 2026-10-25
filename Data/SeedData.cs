using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Helpers;
using ShopSphere.Models;

namespace ShopSphere.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ----- roles -----
        foreach (var role in new[] { "Admin", "Customer" })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        // ----- demo users -----
        await EnsureUser(users, "admin@shopsphere.com", "Store Admin", "Admin@123", "Admin");
        var customer = await EnsureUser(users, "customer@shopsphere.com", "Demo Customer", "Customer@123", "Customer");

        if (await db.Products.AnyAsync()) return;

        // ----- categories -----
        var electronics = new Category { Name = "Electronics", Icon = "laptop", Description = "Gadgets and accessories" };
        var fashion = new Category { Name = "Fashion", Icon = "bag", Description = "Clothing and footwear" };
        var home = new Category { Name = "Home & Kitchen", Icon = "house-door", Description = "Make your home better" };
        var books = new Category { Name = "Books", Icon = "book", Description = "Learn something new" };
        var sports = new Category { Name = "Sports", Icon = "bicycle", Description = "Fitness and outdoor" };
        db.Categories.AddRange(electronics, fashion, home, books, sports);
        await db.SaveChangesAsync();

        // ----- products -----
        var products = new List<Product>
        {
            P("Wireless Noise-Cancelling Headphones", "Over-ear Bluetooth headphones with 30 hour battery, deep bass and active noise cancelling.", 4999, 20, 25, electronics, true),
            P("Smart Fitness Watch", "Heart rate, sleep tracking, 7 day battery and water resistance. Works with Android and iOS.", 2999, 30, 40, electronics, true),
            P("Portable Bluetooth Speaker", "Pocket sized speaker with 12 hour playtime and powerful 360 degree sound.", 1799, 15, 60, electronics, false),
            P("Mechanical Gaming Keyboard", "RGB backlit keyboard with tactile switches and anti-ghosting for gamers and typists.", 3499, 10, 18, electronics, true),
            P("20000 mAh Power Bank", "Fast charging power bank with dual USB ports and USB-C. Charges a phone 4 times.", 1499, 25, 80, electronics, false),
            P("Classic Cotton T-Shirt", "Soft 100% cotton round neck t-shirt. Comfortable fit for everyday wear.", 499, 40, 120, fashion, false),
            P("Slim Fit Denim Jeans", "Stretchable denim with a modern slim fit. Durable stitching and classic five-pocket style.", 1499, 25, 70, fashion, true),
            P("Running Shoes", "Lightweight breathable mesh shoes with cushioned sole for long distance comfort.", 2499, 35, 50, fashion, true),
            P("Leather Wallet", "Genuine leather bi-fold wallet with card slots and coin pocket.", 799, 20, 90, fashion, false),
            P("Non-Stick Cookware Set", "5 piece non-stick set with cool-touch handles. Suitable for gas and induction.", 2599, 30, 35, home, true),
            P("Stainless Steel Water Bottle", "1 litre insulated bottle keeps drinks cold for 24 hours and hot for 12 hours.", 699, 10, 150, home, false),
            P("LED Desk Lamp", "Adjustable desk lamp with 3 colour modes, touch control and USB charging port.", 999, 20, 45, home, false),
            P("Electric Kettle 1.5L", "Fast boiling stainless steel kettle with auto shut-off and boil-dry protection.", 1199, 25, 55, home, false),
            P("Clean Code (Paperback)", "A handbook of agile software craftsmanship. Learn to write readable and maintainable code.", 899, 15, 40, books, true),
            P("Atomic Habits", "Tiny changes, remarkable results. A proven way to build good habits and break bad ones.", 499, 20, 100, books, true),
            P("The Pragmatic Programmer", "Your journey to mastery. Practical advice for software developers.", 1099, 10, 30, books, false),
            P("Yoga Mat (6mm)", "Non-slip, extra thick yoga mat with carrying strap. Ideal for yoga and workouts.", 699, 30, 85, sports, false),
            P("Adjustable Dumbbell Set", "Pair of adjustable dumbbells, 2 to 10 kg each, with a safe locking system.", 3299, 15, 20, sports, true),
            P("Badminton Racket Pair", "Lightweight graphite racket pair with 3 shuttlecocks and a carry bag.", 1299, 20, 65, sports, false),
            P("Skipping Rope", "Tangle-free adjustable speed rope with comfortable grips. Great cardio workout.", 249, 0, 200, sports, false),
        };
        // spread the creation dates so "newest" sorting looks natural
        for (int i = 0; i < products.Count; i++)
            products[i].CreatedAt = DateTime.UtcNow.AddDays(-(products.Count - i));

        var rnd = new Random(7);
        foreach (var p in products)
        {
            p.RatingAverage = Math.Round(3.6 + rnd.NextDouble() * 1.4, 1);
            p.ReviewCount = rnd.Next(3, 60);
        }
        db.Products.AddRange(products);

        // ----- coupons -----
        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", Description = "10% off on orders above ₹500 (max ₹300)", DiscountPercent = 10, MinOrderAmount = 500, MaxDiscount = 300 },
            new Coupon { Code = "FLAT100", Description = "Flat ₹100 off on orders above ₹999", FlatAmount = 100, MinOrderAmount = 999 },
            new Coupon { Code = "SAVE20", Description = "20% off on orders above ₹2000 (max ₹1000)", DiscountPercent = 20, MinOrderAmount = 2000, MaxDiscount = 1000 });

        await db.SaveChangesAsync();

        // ----- a few demo orders so the admin dashboard has data -----
        var statuses = new[]
        {
            OrderStatus.Delivered, OrderStatus.Delivered, OrderStatus.Shipped, OrderStatus.Processing,
            OrderStatus.Pending, OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.Delivered,
            OrderStatus.Processing, OrderStatus.Pending
        };

        for (int i = 0; i < statuses.Length; i++)
        {
            var created = DateTime.UtcNow.AddDays(-rnd.Next(0, 7)).AddHours(-rnd.Next(0, 18));
            var order = new Order
            {
                UserId = customer.Id,
                OrderNumber = $"SS{created:yyMMdd}{1000 + i}",
                CreatedAt = created,
                Status = statuses[i],
                ShippingName = customer.FullName,
                Phone = "9876543210",
                Address = "12, Park Street, Adajan",
                City = "Surat",
                PaymentMethod = i % 2 == 0 ? "UPI" : "COD"
            };

            foreach (var p in products.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4)))
            {
                int qty = rnd.Next(1, 3);
                if (p.Stock < qty) continue;
                order.Items.Add(new OrderItem { ProductId = p.Id, ProductName = p.Name, UnitPrice = p.FinalPrice, Quantity = qty });
                if (order.Status != OrderStatus.Cancelled) p.Stock -= qty;
            }
            if (order.Items.Count == 0) continue;

            order.Subtotal = order.Items.Sum(x => x.UnitPrice * x.Quantity);
            order.Shipping = order.Subtotal >= ShopSettings.FreeShippingThreshold ? 0 : ShopSettings.ShippingFee;
            order.Total = order.Subtotal + order.Shipping;
            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
    }

    private static Product P(string name, string desc, decimal price, int discount, int stock, Category cat, bool featured)
    {
        var p = new Product
        {
            Name = name,
            Description = desc,
            Price = price,
            DiscountPercent = discount,
            Stock = stock,
            Category = cat,
            IsFeatured = featured
        };
        p.RecalculatePrice();
        return p;
    }

    private static async Task<ApplicationUser> EnsureUser(
        UserManager<ApplicationUser> users, string email, string fullName, string password, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user != null) return user;

        user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Could not create demo user: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, role);
        return user;
    }
}
