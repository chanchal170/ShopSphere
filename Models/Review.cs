using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models;

public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WishlistItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime AddedOn { get; set; } = DateTime.UtcNow;
}
