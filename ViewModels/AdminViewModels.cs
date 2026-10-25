using ShopSphere.Models;

namespace ShopSphere.ViewModels;

public class AdminDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueToday { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public int PendingOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }

    public List<string> DayLabels { get; set; } = new();
    public List<decimal> DayRevenue { get; set; } = new();
    public List<string> StatusLabels { get; set; } = new();
    public List<int> StatusCounts { get; set; } = new();

    public List<(string Name, int Quantity)> TopProducts { get; set; } = new();
    public List<Product> LowStock { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
}
