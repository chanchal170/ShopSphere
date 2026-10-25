namespace ShopSphere.Models;

// Small objects returned by the REST API (so we never expose database entities directly)
public record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int DiscountPercent,
    decimal FinalPrice,
    int Stock,
    string Category,
    string? ImageUrl,
    double Rating,
    int ReviewCount);

public record CategoryDto(int Id, string Name, int ProductCount);

public record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);
