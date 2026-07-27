namespace CompareEfAndDapper.Web.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for EF Core (Ignored in Dapper POCO direct queries or handled via multi-mapping)
    public Category? Category { get; set; }
}
