using System.Data;
using CompareEfAndDapper.Web.Models;
using Dapper;

namespace CompareEfAndDapper.Web.Data;

public class DapperRepository
{
    private readonly Func<IDbConnection> _connectionFactory;

    public DapperRepository(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory();
        const string sql = "SELECT Id, Name, Sku, Price, Stock, CategoryId, CreatedAt FROM Products WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Product>> GetProductsWithFilterAsync(int categoryId, decimal minPrice, int limit)
    {
        using var connection = _connectionFactory();
        const string sql = @"
            SELECT Id, Name, Sku, Price, Stock, CategoryId, CreatedAt 
            FROM Products 
            WHERE CategoryId = @CategoryId AND Price >= @MinPrice 
            ORDER BY Price DESC 
            LIMIT @Limit"; // Note: For SQL Server compatibility, DatabaseInitializer handles TOP/LIMIT dialect or SQL standard

        // Standard SQL query with TOP / LIMIT handled dynamically depending on DB connection provider
        var providerName = connection.GetType().Name;
        var dialectSql = providerName.Contains("Sqlite")
            ? sql
            : @"SELECT TOP (@Limit) Id, Name, Sku, Price, Stock, CategoryId, CreatedAt 
               FROM Products 
               WHERE CategoryId = @CategoryId AND Price >= @MinPrice 
               ORDER BY Price DESC";

        return await connection.QueryAsync<Product>(dialectSql, new { CategoryId = categoryId, MinPrice = minPrice, Limit = limit });
    }

    public async Task<IEnumerable<Product>> GetProductsWithCategoryAsync(int limit)
    {
        using var connection = _connectionFactory();
        var providerName = connection.GetType().Name;
        var sql = providerName.Contains("Sqlite")
            ? @"SELECT p.Id, p.Name, p.Sku, p.Price, p.Stock, p.CategoryId, p.CreatedAt,
                       c.Id, c.Name, c.Description, c.CreatedAt
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                LIMIT @Limit"
            : @"SELECT TOP (@Limit) p.Id, p.Name, p.Sku, p.Price, p.Stock, p.CategoryId, p.CreatedAt,
                       c.Id, c.Name, c.Description, c.CreatedAt
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id";

        return await connection.QueryAsync<Product, Category, Product>(
            sql,
            (product, category) =>
            {
                product.Category = category;
                return product;
            },
            new { Limit = limit },
            splitOn: "Id"
        );
    }

    public async Task<int> BulkInsertProductsAsync(IEnumerable<Product> products)
    {
        using var connection = _connectionFactory();
        const string sql = @"
            INSERT INTO Products (Name, Sku, Price, Stock, CategoryId, CreatedAt) 
            VALUES (@Name, @Sku, @Price, @Stock, @CategoryId, @CreatedAt)";
        
        return await connection.ExecuteAsync(sql, products);
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        using var connection = _connectionFactory();
        const string sql = @"
            UPDATE Products 
            SET Name = @Name, Price = @Price, Stock = @Stock 
            WHERE Id = @Id";

        var affected = await connection.ExecuteAsync(sql, product);
        return affected > 0;
    }

    public async Task<int> GetProductCountAsync()
    {
        using var connection = _connectionFactory();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Products");
    }
}
