using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Controllers;

public class ShopController : Controller
{
    private const int PageSize = 9;
    private static readonly char[] Splitters = { ' ', ',', ';', '/', '-', '+', '.', '\t' };

    private readonly AppDbContext _db;

    public ShopController(AppDbContext db) => _db = db;

    // full page
    public async Task<IActionResult> Index(string? q, int? category, string? sort, decimal? min, decimal? max, int page = 1)
        => View(await BuildList(q, category, sort, min, max, page));

    // AJAX: only the product grid (partial view)
    public async Task<IActionResult> List(string? q, int? category, string? sort, decimal? min, decimal? max, int page = 1)
        => PartialView("_ProductGrid", await BuildList(q, category, sort, min, max, page));

    // AJAX: quick suggestions shown under the navbar search box while typing
    [HttpGet]
    public async Task<IActionResult> Suggest(string? term)
    {
        var tokens = Tokenize(term);
        if (tokens.Count == 0) return Json(Array.Empty<object>());

        var query = ApplySearch(_db.Products.AsNoTracking().Where(p => p.IsActive), tokens, includeDescription: false);
        var first = tokens[0];

        var items = await query
            .OrderByDescending(p => p.Name.ToLower().StartsWith(first) ? 1 : 0)
            .ThenByDescending(p => p.RatingAverage)
            .Take(6)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Category = p.Category!.Name,
                Price = p.FinalPrice,
                p.ImageUrl
            })
            .ToListAsync();

        return Json(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (product == null) return NotFound();

        var vm = new ProductDetailsViewModel
        {
            Product = product,
            Reviews = await _db.Reviews.AsNoTracking()
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync(),
            Related = await _db.Products.AsNoTracking().Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive)
                .OrderByDescending(p => p.RatingAverage)
                .Take(4)
                .ToListAsync()
        };

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            vm.WishlistIds = (await _db.WishlistItems.Where(w => w.UserId == userId)
                .Select(w => w.ProductId).ToListAsync()).ToHashSet();
            vm.InWishlist = vm.WishlistIds.Contains(id);
            vm.MyReview = await _db.Reviews.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ProductId == id && r.UserId == userId);
        }

        ViewData["Title"] = product.Name;
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddReview(NewReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please choose a rating from 1 to 5 stars.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        var product = await _db.Products.FindAsync(model.ProductId);
        if (product == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var name = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Customer";

        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.UserId == userId);
        if (review == null)
        {
            review = new Review { ProductId = model.ProductId, UserId = userId, UserName = name };
            _db.Reviews.Add(review);
        }

        review.Rating = model.Rating;
        review.Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
        review.CreatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // refresh the product's rating summary
        product.ReviewCount = await _db.Reviews.CountAsync(r => r.ProductId == product.Id);
        product.RatingAverage = Math.Round(await _db.Reviews.Where(r => r.ProductId == product.Id).AverageAsync(r => r.Rating), 1);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Thank you! Your review has been saved.";
        return RedirectToAction(nameof(Details), new { id = model.ProductId });
    }

    // ---------------- search helpers ----------------

    // "Wireless  HEADPHONES," -> ["wireless", "headphones"]
    private static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var all = text.Trim().ToLowerInvariant()
            .Split(Splitters, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Take(6)
            .ToList();

        // ignore one-letter words ("t" in "t-shirt") unless that is all the user typed
        var useful = all.Where(t => t.Length >= 2).ToList();
        return useful.Count > 0 ? useful : all;
    }

    // simple plural handling: shoes -> shoe, watches -> watch, batteries -> battery
    private static string Stem(string word)
    {
        if (word.Length > 4 && word.EndsWith("ies")) return word[..^3] + "y";
        if (word.Length > 4 && (word.EndsWith("ches") || word.EndsWith("shes") || word.EndsWith("sses") || word.EndsWith("xes")))
            return word[..^2];
        if (word.Length > 3 && word.EndsWith("s") && !word.EndsWith("ss")) return word[..^1];
        return word;
    }

    // Every word the user typed must be found in the product name, category or (optionally) description.
    private static IQueryable<Product> ApplySearch(IQueryable<Product> query, List<string> tokens, bool includeDescription)
    {
        foreach (var token in tokens)
        {
            var a = token;
            var b = Stem(token);
            var useDescription = includeDescription && a.Length >= 3;

            query = query.Where(p =>
                p.Name.ToLower().Contains(a) || p.Name.ToLower().Contains(b) ||
                p.Category!.Name.ToLower().Contains(a) || p.Category!.Name.ToLower().Contains(b) ||
                (useDescription && p.Description != null &&
                    (p.Description.ToLower().Contains(a) || p.Description.ToLower().Contains(b))));
        }
        return query;
    }

    // ---------- search + filter + sort + paging ----------
    private async Task<ProductListViewModel> BuildList(string? q, int? category, string? sort, decimal? min, decimal? max, int page)
    {
        sort = string.IsNullOrWhiteSpace(sort) ? "relevance" : sort;
        var tokens = Tokenize(q);

        var query = _db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive);

        if (tokens.Count > 0) query = ApplySearch(query, tokens, includeDescription: true);
        if (category.HasValue) query = query.Where(p => p.CategoryId == category.Value);
        if (min.HasValue) query = query.Where(p => p.FinalPrice >= min.Value);
        if (max.HasValue) query = query.Where(p => p.FinalPrice <= max.Value);

        IOrderedQueryable<Product> ordered;
        switch (sort)
        {
            case "price_asc": ordered = query.OrderBy(p => p.FinalPrice); break;
            case "price_desc": ordered = query.OrderByDescending(p => p.FinalPrice); break;
            case "rating": ordered = query.OrderByDescending(p => p.RatingAverage).ThenByDescending(p => p.ReviewCount); break;
            case "discount": ordered = query.OrderByDescending(p => p.DiscountPercent); break;
            case "name": ordered = query.OrderBy(p => p.Name); break;
            case "newest": ordered = query.OrderByDescending(p => p.CreatedAt); break;
            default:
                if (tokens.Count > 0)
                {
                    // best match first: exact name > name starts with > name contains phrase > category match
                    var phrase = string.Join(' ', tokens);
                    var first = tokens[0];
                    ordered = query
                        .OrderByDescending(p =>
                            (p.Name.ToLower() == phrase ? 8 : 0) +
                            (p.Name.ToLower().StartsWith(first) ? 4 : 0) +
                            (p.Name.ToLower().Contains(phrase) ? 3 : 0) +
                            (p.Category!.Name.ToLower().Contains(first) ? 1 : 0))
                        .ThenByDescending(p => p.RatingAverage);
                }
                else
                {
                    ordered = query.OrderByDescending(p => p.CreatedAt);
                }
                break;
        }

        var total = await ordered.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var vm = new ProductListViewModel
        {
            Products = await ordered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            Query = q?.Trim(),
            CategoryId = category,
            Sort = sort,
            Min = min,
            Max = max,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total
        };

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            vm.WishlistIds = (await _db.WishlistItems.Where(w => w.UserId == userId)
                .Select(w => w.ProductId).ToListAsync()).ToHashSet();
        }
        return vm;
    }
}
