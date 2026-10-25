using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Price must be between 1 and 10,00,000")]
    public decimal Price { get; set; }

    [Range(0, 90, ErrorMessage = "Discount must be between 0 and 90 %")]
    [Display(Name = "Discount %")]
    public int DiscountPercent { get; set; }

    // Stored price after discount (kept in the database so we can sort and filter by it)
    public decimal FinalPrice { get; set; }

    [Range(0, 100000)]
    public int Stock { get; set; }

    public string? ImageUrl { get; set; }

    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Display(Name = "Featured on home page")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Visible in shop")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Updated whenever a review is added
    public double RatingAverage { get; set; }
    public int ReviewCount { get; set; }

    public List<Review> Reviews { get; set; } = new();

    public void RecalculatePrice() =>
        FinalPrice = Math.Round(Price * (100 - DiscountPercent) / 100m, 2);
}
