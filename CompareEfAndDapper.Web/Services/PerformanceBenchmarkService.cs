using System.Diagnostics;
using CompareEfAndDapper.Web.Data;
using CompareEfAndDapper.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CompareEfAndDapper.Web.Services;

public class BenchmarkResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty; // "EF Core (Tracking)", "EF Core (AsNoTracking)", "Dapper"
    public double ElapsedMilliseconds { get; set; }
    public double ElapsedMicroseconds { get; set; }
    public long AllocatedBytes { get; set; }
    public int ResultCount { get; set; }
    public string SqlExecuted { get; set; } = string.Empty;
    public string CodeSnippet { get; set; } = string.Empty;
    public bool IsColdStart { get; set; }
}

public class BenchmarkComparisonSuite
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BenchmarkResult EfCoreTrackingResult { get; set; } = new();
    public BenchmarkResult EfCoreNoTrackingResult { get; set; } = new();
    public BenchmarkResult DapperResult { get; set; } = new();
    public string SpeedupSummary { get; set; } = string.Empty;
}

public class PerformanceBenchmarkService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DapperRepository _dapperRepository;

    public PerformanceBenchmarkService(IServiceProvider serviceProvider, DapperRepository dapperRepository)
    {
        _serviceProvider = serviceProvider;
        _dapperRepository = dapperRepository;
    }

    public async Task<BenchmarkComparisonSuite> RunScenarioAsync(string scenarioId, int iterations = 10, bool isColdStart = false)
    {
        return scenarioId.ToLower() switch
        {
            "single-read" => await RunSingleReadScenarioAsync(iterations, isColdStart),
            "filter-query" => await RunFilterQueryScenarioAsync(iterations, isColdStart),
            "join-query" => await RunJoinQueryScenarioAsync(iterations, isColdStart),
            "bulk-insert" => await RunBulkInsertScenarioAsync(iterations, isColdStart),
            "update" => await RunUpdateScenarioAsync(iterations, isColdStart),
            _ => await RunSingleReadScenarioAsync(iterations, isColdStart)
        };
    }

    private async Task<BenchmarkComparisonSuite> RunSingleReadScenarioAsync(int iterations, bool isColdStart)
    {
        const int targetId = 42;

        // 1. EF Core Tracking (Best Native Practice: FindAsync uses Identity Map + Primary Key optimization)
        AppDbContext.ClearLogs();
        var efTrackingMetrics = await MeasureAsync(async () =>
        {
            Product? item = null;
            for (int i = 0; i < iterations; i++)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                item = await db.Products.FindAsync(targetId);
            }
            return (item != null ? 1 : 0, AppDbContext.LastExecutedSql ?? "SELECT * FROM Products WHERE Id = @p0");
        });

        // 2. EF Core NoTracking (Best Native Practice: AsNoTracking.FirstOrDefaultAsync for direct query)
        AppDbContext.ClearLogs();
        var efNoTrackingMetrics = await MeasureAsync(async () =>
        {
            Product? item = null;
            for (int i = 0; i < iterations; i++)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                item = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == targetId);
            }
            return (item != null ? 1 : 0, AppDbContext.LastExecutedSql ?? "SELECT * FROM Products WHERE Id = @p0");
        });

        // 3. Dapper (Best Native Practice: QuerySingleOrDefaultAsync for single POCO read)
        var dapperMetrics = await MeasureAsync(async () =>
        {
            Product? item = null;
            for (int i = 0; i < iterations; i++)
            {
                item = await _dapperRepository.GetByIdAsync(targetId);
            }
            return (item != null ? 1 : 0, "SELECT Id, Name, Sku, Price, Stock, CategoryId, CreatedAt FROM Products WHERE Id = @Id");
        });

        return BuildSuite(
            "single-read",
            "1. Lấy thông tin đơn lẻ theo Id (Optimal Native Practice)",
            $"Thực thi truy vấn 1 Product theo Id sử dụng phương thức tối ưu nhất của từng thư viện trong {iterations} lần lặp.",
            efTrackingMetrics, efNoTrackingMetrics, dapperMetrics,
            "await db.Products.FindAsync(id); // (EF Core Native Best Practice - Primary Key Lookup)",
            "await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id); // (EF Core NoTracking Best Practice)",
            "await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id }); // (Dapper Native Best Practice)",
            isColdStart
        );
    }

    private async Task<BenchmarkComparisonSuite> RunFilterQueryScenarioAsync(int iterations, bool isColdStart)
    {
        const int categoryId = 2;
        const decimal minPrice = 50m;
        const int limit = 50;

        // 1. EF Core Tracking
        AppDbContext.ClearLogs();
        var efTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var list = await db.Products
                    .Where(p => p.CategoryId == categoryId && p.Price >= minPrice)
                    .OrderByDescending(p => p.Price)
                    .Take(limit)
                    .ToListAsync();
                count = list.Count;
            }
            return (count, AppDbContext.LastExecutedSql ?? "SELECT TOP(50) ... FROM Products WHERE CategoryId = @p0 AND Price >= @p1");
        });

        // 2. EF Core NoTracking
        AppDbContext.ClearLogs();
        var efNoTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var list = await db.Products.AsNoTracking()
                    .Where(p => p.CategoryId == categoryId && p.Price >= minPrice)
                    .OrderByDescending(p => p.Price)
                    .Take(limit)
                    .ToListAsync();
                count = list.Count;
            }
            return (count, AppDbContext.LastExecutedSql ?? "SELECT TOP(50) ... FROM Products WHERE CategoryId = @p0 AND Price >= @p1");
        });

        // 3. Dapper
        var dapperMetrics = await MeasureAsync(async () =>
        {
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var list = await _dapperRepository.GetProductsWithFilterAsync(categoryId, minPrice, limit);
                count = list.Count();
            }
            return (count, "SELECT TOP (@Limit) Id, Name, Sku, Price, Stock, CategoryId FROM Products WHERE CategoryId = @CategoryId AND Price >= @MinPrice ORDER BY Price DESC");
        });

        return BuildSuite(
            "filter-query",
            "2. Truy vấn danh sách có Lọc & Sắp xếp (Filtered Query)",
            $"Thực thi truy vấn danh sách 50 sản phẩm có điều kiện CategoryId & Price trong {iterations} lần lặp.",
            efTrackingMetrics, efNoTrackingMetrics, dapperMetrics,
            "await db.Products.Where(p => p.CategoryId == catId && p.Price >= minPrice).OrderByDescending(p => p.Price).Take(50).ToListAsync();",
            "await db.Products.AsNoTracking().Where(p => p.CategoryId == catId && p.Price >= minPrice).OrderByDescending(p => p.Price).Take(50).ToListAsync();",
            "await connection.QueryAsync<Product>(sql, new { CategoryId = catId, MinPrice = minPrice, Limit = 50 });",
            isColdStart
        );
    }

    private async Task<BenchmarkComparisonSuite> RunJoinQueryScenarioAsync(int iterations, bool isColdStart)
    {
        const int limit = 50;

        // 1. EF Core Tracking
        AppDbContext.ClearLogs();
        var efTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var list = await db.Products
                    .Include(p => p.Category)
                    .Take(limit)
                    .ToListAsync();
                count = list.Count;
            }
            return (count, AppDbContext.LastExecutedSql ?? "SELECT TOP(50) [p].[Id], ... FROM [Products] AS [p] INNER JOIN [Categories] AS [c] ON [p].[CategoryId] = [c].[Id]");
        });

        // 2. EF Core NoTracking
        AppDbContext.ClearLogs();
        var efNoTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var list = await db.Products.AsNoTracking()
                    .Include(p => p.Category)
                    .Take(limit)
                    .ToListAsync();
                count = list.Count;
            }
            return (count, AppDbContext.LastExecutedSql ?? "SELECT TOP(50) [p].[Id], ... FROM [Products] AS [p] INNER JOIN [Categories] AS [c] ON [p].[CategoryId] = [c].[Id]");
        });

        // 3. Dapper
        var dapperMetrics = await MeasureAsync(async () =>
        {
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var list = await _dapperRepository.GetProductsWithCategoryAsync(limit);
                count = list.Count();
            }
            return (count, "SELECT TOP (@Limit) p.*, c.* FROM Products p INNER JOIN Categories c ON p.CategoryId = c.Id");
        });

        return BuildSuite(
            "join-query",
            "3. Truy vấn Join 2 bảng Multi-mapping (Products + Categories)",
            $"Thực thi Inner Join lấy {limit} sản phẩm kèm thông tin Category trong {iterations} lần lặp.",
            efTrackingMetrics, efNoTrackingMetrics, dapperMetrics,
            "await db.Products.Include(p => p.Category).Take(50).ToListAsync();",
            "await db.Products.AsNoTracking().Include(p => p.Category).Take(50).ToListAsync();",
            "await connection.QueryAsync<Product, Category, Product>(sql, (p, c) => { p.Category = c; return p; }, new { Limit = 50 }, splitOn: \"Id\");",
            isColdStart
        );
    }

    private async Task<BenchmarkComparisonSuite> RunBulkInsertScenarioAsync(int iterations, bool isColdStart)
    {
        int batchSize = 100;

        List<Product> GenerateBatch(int count)
        {
            var list = new List<Product>();
            var random = new Random();
            for (int i = 0; i < count; i++)
            {
                list.Add(new Product
                {
                    Name = $"Batch Temp Product #{Guid.NewGuid().ToString()[..8]}",
                    Sku = $"SKU-TEMP-{Guid.NewGuid().ToString()[..8]}",
                    Price = 99.99m,
                    Stock = 50,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return list;
        }

        // 1. EF Core Tracking
        AppDbContext.ClearLogs();
        var efTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var batch = GenerateBatch(batchSize);
            await db.Products.AddRangeAsync(batch);
            var inserted = await db.SaveChangesAsync();
            return (inserted, AppDbContext.LastExecutedSql ?? "INSERT INTO Products (Name, Sku, ...) VALUES (@p0, @p1, ...)");
        });

        // 2. EF Core NoTracking (Range add without change tracking lookup optimization)
        AppDbContext.ClearLogs();
        var efNoTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false; // Optimize EF batch
            var batch = GenerateBatch(batchSize);
            await db.Products.AddRangeAsync(batch);
            var inserted = await db.SaveChangesAsync();
            return (inserted, AppDbContext.LastExecutedSql ?? "INSERT INTO Products (Name, Sku, ...) VALUES (@p0, @p1, ...)");
        });

        // 3. Dapper
        var dapperMetrics = await MeasureAsync(async () =>
        {
            var batch = GenerateBatch(batchSize);
            var inserted = await _dapperRepository.BulkInsertProductsAsync(batch);
            return (inserted, "INSERT INTO Products (Name, Sku, Price, Stock, CategoryId, CreatedAt) VALUES (@Name, @Sku, @Price, @Stock, @CategoryId, @CreatedAt)");
        });

        return BuildSuite(
            "bulk-insert",
            "4. Thêm mới hàng loạt dữ liệu (Bulk Insert 100 bản ghi)",
            $"Tạo mới và chèn {batchSize} bản ghi vào CSDL.",
            efTrackingMetrics, efNoTrackingMetrics, dapperMetrics,
            "await db.Products.AddRangeAsync(batch);\nawait db.SaveChangesAsync();",
            "db.ChangeTracker.AutoDetectChangesEnabled = false;\nawait db.Products.AddRangeAsync(batch);\nawait db.SaveChangesAsync();",
            "await connection.ExecuteAsync(insertSql, batchArray);",
            isColdStart
        );
    }

    private async Task<BenchmarkComparisonSuite> RunUpdateScenarioAsync(int iterations, bool isColdStart)
    {
        const int targetId = 1;

        // 1. EF Core Tracking
        AppDbContext.ClearLogs();
        var efTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var prod = await db.Products.FindAsync(targetId);
                if (prod != null)
                {
                    prod.Price += 0.01m;
                    count += await db.SaveChangesAsync();
                }
            }
            return (count, AppDbContext.LastExecutedSql ?? "UPDATE Products SET Price = @p0 WHERE Id = @p1");
        });

        // 2. EF Core ExecuteUpdate (Modern EF Core SQL Direct Update)
        AppDbContext.ClearLogs();
        var efNoTrackingMetrics = await MeasureAsync(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                count += await db.Products
                    .Where(p => p.Id == targetId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, p => p.Price + 0.01m));
            }
            return (count, AppDbContext.LastExecutedSql ?? "UPDATE Products SET Price = Price + 0.01 WHERE Id = 1");
        });

        // 3. Dapper
        var dapperMetrics = await MeasureAsync(async () =>
        {
            int count = 0;
            for (int i = 0; i < iterations; i++)
            {
                var prod = await _dapperRepository.GetByIdAsync(targetId);
                if (prod != null)
                {
                    prod.Price += 0.01m;
                    var updated = await _dapperRepository.UpdateProductAsync(prod);
                    if (updated) count++;
                }
            }
            return (count, "UPDATE Products SET Name = @Name, Price = @Price, Stock = @Stock WHERE Id = @Id");
        });

        return BuildSuite(
            "update",
            "5. Cập nhật bản ghi (Update Scenario)",
            $"Thực thi cập nhật thuộc tính Price cho sản phẩm Id={targetId} trong {iterations} lần lặp.",
            efTrackingMetrics, efNoTrackingMetrics, dapperMetrics,
            "var prod = await db.Products.FindAsync(id);\nprod.Price += 0.01m;\nawait db.SaveChangesAsync(); // (EF ChangeTracker tự sinh UPDATE)",
            "await db.Products.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, p => p.Price + 0.01m)); // (EF Direct Update)",
            "var prod = await repo.GetByIdAsync(id);\nprod.Price += 0.01m;\nawait connection.ExecuteAsync(updateSql, prod); // (Explicit UPDATE)",
            isColdStart
        );
    }

    private async Task<(double ms, double us, long allocatedBytes, int count, string sql)> MeasureAsync(Func<Task<(int count, string sql)>> action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();

        var (count, sql) = await action();

        sw.Stop();
        long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
        long allocated = Math.Max(0, bytesAfter - bytesBefore);

        double ms = sw.Elapsed.TotalMilliseconds;
        double us = sw.Elapsed.TotalMicroseconds;

        return (ms, us, allocated, count, sql);
    }

    private BenchmarkComparisonSuite BuildSuite(
        string id, string title, string description,
        (double ms, double us, long allocatedBytes, int count, string sql) efTrack,
        (double ms, double us, long allocatedBytes, int count, string sql) efNoTrack,
        (double ms, double us, long allocatedBytes, int count, string sql) dapper,
        string codeEfTrack, string codeEfNoTrack, string codeDapper,
        bool isColdStart)
    {
        var efTrackRes = new BenchmarkResult
        {
            ScenarioName = title,
            Mode = "EF Core (Tracking)",
            ElapsedMilliseconds = Math.Round(efTrack.ms, 3),
            ElapsedMicroseconds = Math.Round(efTrack.us, 1),
            AllocatedBytes = efTrack.allocatedBytes,
            ResultCount = efTrack.count,
            SqlExecuted = efTrack.sql,
            CodeSnippet = codeEfTrack,
            IsColdStart = isColdStart
        };

        var efNoTrackRes = new BenchmarkResult
        {
            ScenarioName = title,
            Mode = "EF Core (AsNoTracking / Direct)",
            ElapsedMilliseconds = Math.Round(efNoTrack.ms, 3),
            ElapsedMicroseconds = Math.Round(efNoTrack.us, 1),
            AllocatedBytes = efNoTrack.allocatedBytes,
            ResultCount = efNoTrack.count,
            SqlExecuted = efNoTrack.sql,
            CodeSnippet = codeEfNoTrack,
            IsColdStart = isColdStart
        };

        var dapperRes = new BenchmarkResult
        {
            ScenarioName = title,
            Mode = "Dapper (Micro-ORM)",
            ElapsedMilliseconds = Math.Round(dapper.ms, 3),
            ElapsedMicroseconds = Math.Round(dapper.us, 1),
            AllocatedBytes = dapper.allocatedBytes,
            ResultCount = dapper.count,
            SqlExecuted = dapper.sql,
            CodeSnippet = codeDapper,
            IsColdStart = isColdStart
        };

        double ratio = efTrackRes.ElapsedMilliseconds > 0 
            ? Math.Round(efTrackRes.ElapsedMilliseconds / Math.Max(0.001, dapperRes.ElapsedMilliseconds), 2)
            : 1.0;

        string speedup = ratio > 1.0 
            ? $"⚡ Dapper nhanh hơn EF Core (Tracking) khoảng **{ratio}x** trong kịch bản này."
            : "⚡ Thời gian thực thi giữa EF Core và Dapper gần như tương đương.";

        return new BenchmarkComparisonSuite
        {
            ScenarioId = id,
            Title = title,
            Description = description,
            EfCoreTrackingResult = efTrackRes,
            EfCoreNoTrackingResult = efNoTrackRes,
            DapperResult = dapperRes,
            SpeedupSummary = speedup
        };
    }
}
