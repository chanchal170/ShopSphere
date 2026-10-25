using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories.AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Create([Bind("Name,Icon,Description")] Category category)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter a category name and icon.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.Trim().ToLower()))
        {
            TempData["Error"] = "A category with this name already exists.";
            return RedirectToAction(nameof(Index));
        }

        category.Name = category.Name.Trim();
        category.Icon = category.Icon.Trim();
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.Name}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        return category == null ? NotFound() : View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Icon,Description")] Category input)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (!ModelState.IsValid)
        {
            input.Id = id;
            return View(input);
        }

        category.Name = input.Name.Trim();
        category.Icon = input.Icon.Trim();
        category.Description = input.Description;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
        {
            TempData["Error"] = "This category still has products. Move or delete them first.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Category '{category.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }
}
