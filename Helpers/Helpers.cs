using System.Globalization;
using System.Text.Json;
using ShopSphere.Models;

namespace ShopSphere.Helpers;

// Store-wide rules in one place
public static class ShopSettings
{
    public const decimal FreeShippingThreshold = 500m;
    public const decimal ShippingFee = 49m;
    public const int MaxQuantityPerItem = 10;
}

// Small formatting helpers used by the views
public static class Fmt
{
    private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");

    public static string Money(decimal value) =>
        "₹" + value.ToString(value % 1 == 0 ? "N0" : "N2", India);

    public static string Date(DateTime utc) => utc.ToLocalTime().ToString("dd MMM yyyy");

    public static string Stamp(DateTime utc) => utc.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");

    // A stable colour (0-359) for a piece of text, used for placeholder images
    public static int Hue(string text) => text.Sum(c => (int)c) * 7 % 360;

    public static string StatusClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Processing => "bg-info text-dark",
        OrderStatus.Shipped => "bg-primary",
        OrderStatus.Delivered => "bg-success",
        _ => "bg-danger"
    };
}

// Lets us store small objects in the session as JSON
public static class SessionJsonExtensions
{
    public static void SetJson<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));

    public static T? GetJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
