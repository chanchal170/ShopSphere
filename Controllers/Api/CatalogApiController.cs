using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Controllers.Api;

/// <summary>Public read-only catalogue API. Open /swagger while running in Development to try it.</summary>
[ApiController]
[Route("api")]
public class CatalogApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogApiController(AppDbContext db) => _db = db;

    /// <summary>List products with optional search, category filter and paging.</summary>
    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        string? q, int? categoryId, int page = 1, int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.Price, p.DiscountPercent, p.FinalPrice, p.Stock,
                p.Category != null ? p.Category.Name : "", p.ImageUrl, p.RatingAverage, p.ReviewCount))
            .ToListAsync();

        return new PagedResult<ProductDto>(total, page, pageSize, items);
    }

    /// <summary>Get a single product by id.</summary>
    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var dto = await _db.Products.AsNoTracking()
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.Price, p.DiscountPercent, p.FinalPrice, p.Stock,
                p.Category != null ? p.Category.Name : "", p.ImageUrl, p.RatingAverage, p.ReviewCount))
            .FirstOrDefaultAsync();

        if (dto == null) return NotFound(new { message = "Product not found." });
        return dto;
    }

    /// <summary>List categories with the number of products in each.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        return await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Products.Count(p => p.IsActive)))
            .ToListAsync();
    }

    /// <summary>Store statistics (Admin login required - the API uses the same login cookie).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        return Ok(new
        {
            products = await _db.Products.CountAsync(),
            categories = await _db.Categories.CountAsync(),
            orders = await _db.Orders.CountAsync(),
            pendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending)
        });
    }
}
