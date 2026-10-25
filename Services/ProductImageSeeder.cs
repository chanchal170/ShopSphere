using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Services;

/// <summary>
/// Gives the sample products real photos.
/// On start-up (in the background, so the site opens immediately) it looks for products without a picture,
/// downloads a matching photo once, saves it in wwwroot/images/products and links it to the product.
///
/// Rules:
///  - Needs internet only for the first download; after that the photos are local files.
///  - If you put your own file named like the slug (for example headphones.jpg) in that folder, it is used instead.
///  - Products you create yourself in the admin panel are never touched.
///  - If a download fails, the product simply keeps its coloured icon. Restart the app to try again.
/// </summary>
public class ProductImageSeeder : BackgroundService
{
    // product name -> (file name without extension, photo search words to try in order)
    private static readonly Dictionary<string, (string Slug, string[] Keywords)> Map = new()
    {
        ["Wireless Noise-Cancelling Headphones"] = ("headphones", new[] { "headphones", "earphones" }),
        ["Smart Fitness Watch"] = ("smart-watch", new[] { "smartwatch", "wristwatch" }),
        ["Portable Bluetooth Speaker"] = ("bluetooth-speaker", new[] { "speaker", "loudspeaker" }),
        ["Mechanical Gaming Keyboard"] = ("keyboard", new[] { "keyboard", "keyboards" }),
        ["20000 mAh Power Bank"] = ("power-bank", new[] { "powerbank", "charger" }),
        ["Classic Cotton T-Shirt"] = ("t-shirt", new[] { "tshirt", "shirt" }),
        ["Slim Fit Denim Jeans"] = ("jeans", new[] { "jeans", "denim" }),
        ["Running Shoes"] = ("running-shoes", new[] { "sneakers", "shoes" }),
        ["Leather Wallet"] = ("wallet", new[] { "wallet", "leather" }),
        ["Non-Stick Cookware Set"] = ("cookware", new[] { "cookware", "pan" }),
        ["Stainless Steel Water Bottle"] = ("water-bottle", new[] { "waterbottle", "bottle" }),
        ["LED Desk Lamp"] = ("desk-lamp", new[] { "desklamp", "lamp" }),
        ["Electric Kettle 1.5L"] = ("kettle", new[] { "kettle", "teapot" }),
        ["Clean Code (Paperback)"] = ("book-clean-code", new[] { "book", "books" }),
        ["Atomic Habits"] = ("book-atomic-habits", new[] { "paperback", "reading" }),
        ["The Pragmatic Programmer"] = ("book-pragmatic", new[] { "library", "bookshelf" }),
        ["Yoga Mat (6mm)"] = ("yoga-mat", new[] { "yogamat", "yoga" }),
        ["Adjustable Dumbbell Set"] = ("dumbbell", new[] { "dumbbell", "weights" }),
        ["Badminton Racket Pair"] = ("badminton", new[] { "badminton", "racket" }),
        ["Skipping Rope"] = ("skipping-rope", new[] { "jumprope", "skippingrope" }),
    };

    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IServiceScopeFactory _scopes;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ProductImageSeeder> _log;

    public ProductImageSeeder(IServiceScopeFactory scopes, IWebHostEnvironment env,
        IHttpClientFactory http, ILogger<ProductImageSeeder> log)
    {
        _scopes = scopes;
        _env = env;
        _http = http;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(1500, stoppingToken);   // let the website start first
            await RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) { /* app is shutting down */ }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Product photos could not be prepared. Products will show icons instead.");
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var folder = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(folder);

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var missing = await db.Products
            .Where(p => p.ImageUrl == null || p.ImageUrl == "")
            .ToListAsync(ct);

        var client = _http.CreateClient("photos");
        int linked = 0, failed = 0;

        foreach (var product in missing)
        {
            if (!Map.TryGetValue(product.Name, out var info)) continue;   // custom product - leave it alone

            var existing = FindFile(folder, info.Slug);
            if (existing == null)
            {
                var saved = await DownloadAsync(client, folder, info.Slug, info.Keywords, ct);
                if (saved == null) { failed++; continue; }
                existing = saved;
            }

            product.ImageUrl = "/images/products/" + Path.GetFileName(existing);
            await db.SaveChangesAsync(ct);   // save one by one so photos appear while you browse
            linked++;
        }

        if (linked > 0 || failed > 0)
            _log.LogInformation("Product photos: {Linked} linked, {Failed} could not be downloaded.", linked, failed);
    }

    private static string? FindFile(string folder, string slug)
    {
        foreach (var ext in Extensions)
        {
            var path = Path.Combine(folder, slug + ext);
            if (File.Exists(path) && new FileInfo(path).Length > 2000) return path;
        }
        return null;
    }

    private async Task<string?> DownloadAsync(HttpClient client, string folder, string slug, string[] keywords, CancellationToken ct)
    {
        // a stable number per product so the same photo is chosen every time
        var seed = (int)(slug.Aggregate(17u, (h, c) => unchecked(h * 31 + c)) % 900) + 1;

        foreach (var word in keywords)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var url = $"https://loremflickr.com/800/800/{word}?lock={seed + attempt * 37}";
                try
                {
                    using var response = await client.GetAsync(url, ct);
                    if (!response.IsSuccessStatusCode) continue;

                    var type = response.Content.Headers.ContentType?.MediaType ?? "";
                    if (!type.StartsWith("image/")) continue;

                    var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                    if (bytes.Length < 8000) continue;   // too small to be a real photo

                    var path = Path.Combine(folder, slug + ".jpg");
                    await File.WriteAllBytesAsync(path, bytes, ct);
                    return path;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Photo download failed for {Slug} ({Word})", slug, word);
                }
            }
        }
        return null;
    }
}
