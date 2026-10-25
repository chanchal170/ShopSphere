using System.ComponentModel.DataAnnotations;
using ShopSphere.Models;

namespace ShopSphere.ViewModels;

public class HomeViewModel
{
    public List<Product> Featured { get; set; } = new();
    public List<Product> NewArrivals { get; set; } = new();
    public List<Product> Deals { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public Dictionary<int, int> CategoryCounts { get; set; } = new();
    public HashSet<int> WishlistIds { get; set; } = new();
    public int ProductCount { get; set; }
    public int CustomerCount { get; set; }
    public int OrderCount { get; set; }
}

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public HashSet<int> WishlistIds { get; set; } = new();

    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public string Sort { get; set; } = "relevance";
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;
    public List<Review> Reviews { get; set; } = new();
    public List<Product> Related { get; set; } = new();
    public HashSet<int> WishlistIds { get; set; } = new();
    public bool InWishlist { get; set; }
    public Review? MyReview { get; set; }
}

public class NewReviewViewModel
{
    public int ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5")]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}
