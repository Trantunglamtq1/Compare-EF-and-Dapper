namespace CompareEfAndDapper.Web.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for EF Core
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
