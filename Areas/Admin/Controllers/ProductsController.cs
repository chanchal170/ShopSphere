using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private const int PageSize = 10;
    private readonly AppDbContext _db;
    private readonly IImageService _images;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext db, IImageService images, ILogger<ProductsController> logger)
    {
        _db = db;
        _images = images;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        ViewBag.Query = q;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        var products = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new Product { Stock = 10 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Price,DiscountPercent,Stock,CategoryId,IsFeatured,IsActive")] Product product,
        IFormFile? image)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategories(product.CategoryId);
            return View(product);
        }

        try
        {
            if (image != null && image.Length > 0) product.ImageUrl = await _images.SaveAsync(image);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategories(product.CategoryId);
            return View(product);
        }

        product.RecalculatePrice();
        product.CreatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Product {Name} created", product.Name);
        TempData["Success"] = $"'{product.Name}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        await LoadCategories(product.CategoryId);
        return View(product);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Name,Description,Price,DiscountPercent,Stock,CategoryId,IsFeatured,IsActive")] Product input,
        IFormFile? image,
        bool removeImage)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        if (!ModelState.IsValid)
        {
            input.Id = id;
            input.ImageUrl = product.ImageUrl;
            await LoadCategories(input.CategoryId);
            return View(input);
        }

        string? newImage = null;
        try
        {
            if (image != null && image.Length > 0) newImage = await _images.SaveAsync(image);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            input.Id = id;
            input.ImageUrl = product.ImageUrl;
            await LoadCategories(input.CategoryId);
            return View(input);
        }

        product.Name = input.Name;
        product.Description = input.Description;
        product.Price = input.Price;
        product.DiscountPercent = input.DiscountPercent;
        product.Stock = input.Stock;
        product.CategoryId = input.CategoryId;
        product.IsFeatured = input.IsFeatured;
        product.IsActive = input.IsActive;
        product.RecalculatePrice();

        if (newImage != null)
        {
            _images.Delete(product.ImageUrl);
            product.ImageUrl = newImage;
        }
        else if (removeImage)
        {
            _images.Delete(product.ImageUrl);
            product.ImageUrl = null;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"'{product.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        // Products that appear in past orders are hidden instead of deleted, so order history stays correct.
        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id))
        {
            product.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"'{product.Name}' is used in past orders, so it was hidden from the shop instead of deleted.";
        }
        else
        {
            _images.Delete(product.ImageUrl);
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"'{product.Name}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategories(int? selected = null)
    {
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selected);
    }
}
