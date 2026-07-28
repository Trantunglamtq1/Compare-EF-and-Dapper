using CompareEfAndDapper.Web.Data;
using CompareEfAndDapper.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompareEfAndDapper.Web.Controllers;

[ApiController]
[Route("api/k6")]
public class K6TestController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly DapperRepository _dapperRepository;
    private readonly SqlCommandRepository _sqlRepository;

    public K6TestController(
        AppDbContext dbContext,
        DapperRepository dapperRepository,
        SqlCommandRepository sqlRepository)
    {
        _dbContext = dbContext;
        _dapperRepository = dapperRepository;
        _sqlRepository = sqlRepository;
    }

    // --- Scenario 1: Single Read ---

    [HttpGet("ef/single-read/{id}")]
    public async Task<IActionResult> EfSingleRead(int id = 42)
    {
        var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet("dapper/single-read/{id}")]
    public async Task<IActionResult> DapperSingleRead(int id = 42)
    {
        var product = await _dapperRepository.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet("sql/single-read/{id}")]
    public async Task<IActionResult> SqlSingleRead(int id = 42)
    {
        var product = await _sqlRepository.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    // --- Scenario 2: Filter Query ---

    [HttpGet("ef/filter-query")]
    public async Task<IActionResult> EfFilterQuery([FromQuery] int categoryId = 1, [FromQuery] decimal minPrice = 100, [FromQuery] int limit = 50)
    {
        var products = await _dbContext.Products.AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.Price >= minPrice)
            .OrderByDescending(p => p.Price)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("dapper/filter-query")]
    public async Task<IActionResult> DapperFilterQuery([FromQuery] int categoryId = 1, [FromQuery] decimal minPrice = 100, [FromQuery] int limit = 50)
    {
        var products = await _dapperRepository.GetProductsWithFilterAsync(categoryId, minPrice, limit);
        return Ok(products);
    }

    [HttpGet("sql/filter-query")]
    public async Task<IActionResult> SqlFilterQuery([FromQuery] int categoryId = 1, [FromQuery] decimal minPrice = 100, [FromQuery] int limit = 50)
    {
        var products = await _sqlRepository.GetProductsWithFilterAsync(categoryId, minPrice, limit);
        return Ok(products);
    }

    // --- Scenario 3: Join Query ---

    [HttpGet("ef/join-query")]
    public async Task<IActionResult> EfJoinQuery([FromQuery] int limit = 50)
    {
        var products = await _dbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("dapper/join-query")]
    public async Task<IActionResult> DapperJoinQuery([FromQuery] int limit = 50)
    {
        var products = await _dapperRepository.GetProductsWithCategoryAsync(limit);
        return Ok(products);
    }

    [HttpGet("sql/join-query")]
    public async Task<IActionResult> SqlJoinQuery([FromQuery] int limit = 50)
    {
        var products = await _sqlRepository.GetProductsWithCategoryAsync(limit);
        return Ok(products);
    }

    // --- Scenario 4: Bulk Insert ---

    [HttpPost("ef/bulk-insert")]
    public async Task<IActionResult> EfBulkInsert([FromQuery] int count = 50)
    {
        var batch = GenerateBatch(count);
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContext.Products.AddRangeAsync(batch);
        var inserted = await _dbContext.SaveChangesAsync();
        return Ok(new { inserted });
    }

    [HttpPost("dapper/bulk-insert")]
    public async Task<IActionResult> DapperBulkInsert([FromQuery] int count = 50)
    {
        var batch = GenerateBatch(count);
        var inserted = await _dapperRepository.BulkInsertProductsAsync(batch);
        return Ok(new { inserted });
    }

    [HttpPost("sql/bulk-insert")]
    public async Task<IActionResult> SqlBulkInsert([FromQuery] int count = 50)
    {
        var batch = GenerateBatch(count);
        var inserted = await _sqlRepository.BulkInsertProductsAsync(batch);
        return Ok(new { inserted });
    }

    // --- Scenario 5: Update ---

    [HttpPut("ef/update/{id}")]
    public async Task<IActionResult> EfUpdate(int id = 1)
    {
        var updated = await _dbContext.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, p => p.Price + 0.01m));

        return Ok(new { updated });
    }

    [HttpPut("dapper/update/{id}")]
    public async Task<IActionResult> DapperUpdate(int id = 1)
    {
        var product = new Product
        {
            Id = id,
            Name = "Updated Name",
            Price = 199.99m,
            Stock = 100
        };
        var updated = await _dapperRepository.UpdateProductAsync(product);
        return Ok(new { updated });
    }

    [HttpPut("sql/update/{id}")]
    public async Task<IActionResult> SqlUpdate(int id = 1)
    {
        var updated = await _sqlRepository.UpdateProductAsync(id, 199.99m);
        return Ok(new { updated });
    }

    private static List<Product> GenerateBatch(int count)
    {
        var list = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new Product
            {
                Name = $"K6 Batch Temp Product #{Guid.NewGuid().ToString()[..8]}",
                Sku = $"SKU-K6-{Guid.NewGuid().ToString()[..8]}",
                Price = 99.99m,
                Stock = 50,
                CategoryId = 1,
                CreatedAt = DateTime.UtcNow
            });
        }
        return list;
    }
}
