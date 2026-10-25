using System.ComponentModel.DataAnnotations;
using ShopSphere.Models;

namespace ShopSphere.ViewModels;

// ---------------- Cart ----------------
public class CartLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public string Icon { get; set; } = "box-seam";
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartSummary
{
    public List<CartLine> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public int ItemCount => Lines.Sum(l => l.Quantity);
}

// ---------------- Checkout ----------------
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Please enter the receiver's name"), StringLength(80)]
    [Display(Name = "Full name")]
    public string ShippingName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10 digit mobile number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required"), StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required"), StringLength(60)]
    public string City { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(COD|UPI|Card)$", ErrorMessage = "Choose a payment method")]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = "COD";

    // filled by the controller, not posted by the form
    public CartSummary? Cart { get; set; }
}
