using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    // Bootstrap icon name, for example "laptop" or "book"
    [Required, StringLength(40)]
    public string Icon { get; set; } = "box-seam";

    [StringLength(200)]
    public string? Description { get; set; }

    public List<Product> Products { get; set; } = new();
}
