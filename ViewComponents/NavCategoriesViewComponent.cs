using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.ViewComponents;

// Category dropdown shown in the navbar
public class NavCategoriesViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public NavCategoriesViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
        => View(await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync());
}
