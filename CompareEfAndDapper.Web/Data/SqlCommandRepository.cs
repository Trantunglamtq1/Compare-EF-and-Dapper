using System.Data;
using System.Data.Common;
using CompareEfAndDapper.Web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace CompareEfAndDapper.Web.Data;

/// <summary>
/// Raw ADO.NET SqlCommand repository — không dùng ORM hay micro-ORM nào.
/// Toàn bộ mapping được thực hiện thủ công bằng DbDataReader.
/// </summary>
public class SqlCommandRepository
{
    private readonly Func<IDbConnection> _connectionFactory;

    public SqlCommandRepository(Func<IDbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // --- Helpers ---

    private DbConnection OpenDbConnection()
    {
        var conn = _connectionFactory();
        if (conn.State != ConnectionState.Open)
            conn.Open();
        return (DbConnection)conn;
    }

    private static bool IsSqlite(DbConnection conn) =>
        conn is SqliteConnection;

    // --- Scenario 1: Single Read ---

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var conn = OpenDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Sku, Price, Stock, CategoryId, CreatedAt FROM Products WHERE Id = @Id";

        var param = cmd.CreateParameter();
        param.ParameterName = "@Id";
        param.Value = id;
        cmd.Parameters.Add(param);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapProduct(reader);
    }

    // --- Scenario 2: Filter Query ---

    public async Task<List<Product>> GetProductsWithFilterAsync(int categoryId, decimal minPrice, int limit)
    {
        await using var conn = OpenDbConnection();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = IsSqlite(conn)
            ? @"SELECT Id, Name, Sku, Price, Stock, CategoryId, CreatedAt 
                FROM Products 
                WHERE CategoryId = @CategoryId AND Price >= @MinPrice 
                ORDER BY Price DESC 
                LIMIT @Limit"
            : @"SELECT TOP (@Limit) Id, Name, Sku, Price, Stock, CategoryId, CreatedAt 
                FROM Products 
                WHERE CategoryId = @CategoryId AND Price >= @MinPrice 
                ORDER BY Price DESC";

        AddParam(cmd, "@CategoryId", categoryId);
        AddParam(cmd, "@MinPrice", minPrice);
        AddParam(cmd, "@Limit", limit);

        var results = new List<Product>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(MapProduct(reader));

        return results;
    }

    // --- Scenario 3: Join Query ---

    public async Task<List<Product>> GetProductsWithCategoryAsync(int limit)
    {
        await using var conn = OpenDbConnection();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = IsSqlite(conn)
            ? @"SELECT p.Id, p.Name, p.Sku, p.Price, p.Stock, p.CategoryId, p.CreatedAt,
                       c.Id AS CatId, c.Name AS CatName, c.Description AS CatDesc
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                LIMIT @Limit"
            : @"SELECT TOP (@Limit) p.Id, p.Name, p.Sku, p.Price, p.Stock, p.CategoryId, p.CreatedAt,
                       c.Id AS CatId, c.Name AS CatName, c.Description AS CatDesc
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id";

        AddParam(cmd, "@Limit", limit);

        var results = new List<Product>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var product = MapProduct(reader);
            product.Category = new Category
            {
                Id       = reader.GetInt32(reader.GetOrdinal("CatId")),
                Name     = reader.GetString(reader.GetOrdinal("CatName")),
                Description = reader.IsDBNull(reader.GetOrdinal("CatDesc"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CatDesc"))
            };
            results.Add(product);
        }

        return results;
    }

    // --- Scenario 4: Bulk Insert ---

    public async Task<int> BulkInsertProductsAsync(IEnumerable<Product> products)
    {
        await using var conn = OpenDbConnection();
        int inserted = 0;

        foreach (var p in products)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Products (Name, Sku, Price, Stock, CategoryId, CreatedAt)
                VALUES (@Name, @Sku, @Price, @Stock, @CategoryId, @CreatedAt)";

            AddParam(cmd, "@Name",       p.Name);
            AddParam(cmd, "@Sku",        p.Sku);
            AddParam(cmd, "@Price",      p.Price);
            AddParam(cmd, "@Stock",      p.Stock);
            AddParam(cmd, "@CategoryId", p.CategoryId);
            AddParam(cmd, "@CreatedAt",  p.CreatedAt.ToString("o")); // ISO 8601

            inserted += await cmd.ExecuteNonQueryAsync();
        }

        return inserted;
    }

    // --- Scenario 5: Update ---

    public async Task<bool> UpdateProductAsync(int id, decimal newPrice)
    {
        await using var conn = OpenDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Products SET Price = @Price WHERE Id = @Id";

        AddParam(cmd, "@Price", newPrice);
        AddParam(cmd, "@Id",    id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    // --- Private helpers ---

    private static Product MapProduct(DbDataReader r) => new()
    {
        Id         = r.GetInt32(r.GetOrdinal("Id")),
        Name       = r.GetString(r.GetOrdinal("Name")),
        Sku        = r.GetString(r.GetOrdinal("Sku")),
        Price      = r.GetDecimal(r.GetOrdinal("Price")),
        Stock      = r.GetInt32(r.GetOrdinal("Stock")),
        CategoryId = r.GetInt32(r.GetOrdinal("CategoryId")),
        CreatedAt  = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt")))
    };

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
