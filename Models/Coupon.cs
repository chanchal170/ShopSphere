using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Description { get; set; }

    // Percent discount (0 if you use a flat amount instead)
    [Range(0, 100)]
    [Display(Name = "Discount %")]
    public int DiscountPercent { get; set; }

    // Flat rupee discount (used when DiscountPercent is 0)
    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Flat amount")]
    public decimal FlatAmount { get; set; }

    // Maximum discount for percent coupons (0 = no limit)
    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Max discount")]
    public decimal MaxDiscount { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Minimum order")]
    public decimal MinOrderAmount { get; set; }

    [Display(Name = "Expires on")]
    public DateTime? ExpiresOn { get; set; }

    // 0 = unlimited
    [Range(0, 1000000)]
    [Display(Name = "Usage limit")]
    public int UsageLimit { get; set; }

    public int TimesUsed { get; set; }

    public bool IsActive { get; set; } = true;
}
