using CompareEfAndDapper.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CompareEfAndDapper.Web.Data;

public static class DatabaseInitializer
{
    public static string ActiveProvider { get; private set; } = "Unknown";
    public static string ConnectionStringInfo { get; private set; } = string.Empty;
    public static bool IsInitialized { get; private set; }

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        ActiveProvider = dbContext.Database.ProviderName ?? "Unknown";
        ConnectionStringInfo = dbContext.Database.GetConnectionString() ?? "None";

        // Ensure database schema is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if DB already has seed data
        if (!await dbContext.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Electronics", Description = "Laptops, Smartphones, Gadgets" },
                new() { Name = "Books & Media", Description = "Technical books, fiction, audiobooks" },
                new() { Name = "Home & Kitchen", Description = "Furniture, appliances, tools" },
                new() { Name = "Clothing & Accessories", Description = "Apparel, shoes, watches" },
                new() { Name = "Sports & Outdoors", Description = "Fitness gear, camping equipment" },
                new() { Name = "Automotive", Description = "Car accessories, spare parts" },
                new() { Name = "Toys & Games", Description = "Board games, puzzles, action figures" },
                new() { Name = "Health & Beauty", Description = "Skincare, vitamins, cosmetics" },
                new() { Name = "Office Supplies", Description = "Stationery, desks, paper" },
                new() { Name = "Groceries", Description = "Organic foods, snacks, beverages" }
            };

            await dbContext.Categories.AddRangeAsync(categories);
            await dbContext.SaveChangesAsync();

            var random = new Random(42);
            var products = new List<Product>();

            for (int i = 1; i <= 1000; i++)
            {
                var category = categories[random.Next(categories.Count)];
                products.Add(new Product
                {
                    Name = $"Product Item #{i} ({category.Name})",
                    Sku = $"SKU-PROD-{i:D5}",
                    Price = (decimal)(random.NextDouble() * 500 + 5),
                    Stock = random.Next(1, 200),
                    CategoryId = category.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 10000))
                });
            }

            await dbContext.Products.AddRangeAsync(products);
            await dbContext.SaveChangesAsync();
        }

        IsInitialized = true;
    }
}
