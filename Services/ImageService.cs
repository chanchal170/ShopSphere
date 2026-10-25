namespace ShopSphere.Services;

public interface IImageService
{
    // Saves an uploaded picture and returns its public URL (throws InvalidOperationException for bad files)
    Task<string> SaveAsync(IFormFile file);
    void Delete(string? url);
}

public class ImageService : IImageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

    private readonly IWebHostEnvironment _env;

    public ImageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(IFormFile file)
    {
        if (file.Length == 0) throw new InvalidOperationException("The selected image is empty.");
        if (file.Length > MaxBytes) throw new InvalidOperationException("Image is too large. Maximum size is 2 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Only JPG, PNG, WEBP or GIF images are allowed.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        await using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/products/")) return;

        var path = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
    }
}
