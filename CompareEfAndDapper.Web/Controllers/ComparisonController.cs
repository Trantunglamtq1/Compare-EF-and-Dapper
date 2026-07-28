using CompareEfAndDapper.Web.Data;
using CompareEfAndDapper.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompareEfAndDapper.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComparisonController : ControllerBase
{
    private readonly PerformanceBenchmarkService _benchmarkService;
    private readonly ExecutionFlowAnalyzer _flowAnalyzer;
    private readonly AppDbContext _dbContext;

    public ComparisonController(
        PerformanceBenchmarkService benchmarkService,
        ExecutionFlowAnalyzer flowAnalyzer,
        AppDbContext dbContext)
    {
        _benchmarkService = benchmarkService;
        _flowAnalyzer = flowAnalyzer;
        _dbContext = dbContext;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        int productCount = await _dbContext.Products.CountAsync();
        int categoryCount = await _dbContext.Categories.CountAsync();

        return Ok(new
        {
            provider = DatabaseInitializer.ActiveProvider,
            connectionString = DatabaseInitializer.ConnectionStringInfo,
            isInitialized = DatabaseInitializer.IsInitialized,
            totalProducts = productCount,
            totalCategories = categoryCount,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    [HttpGet("scenarios")]
    public IActionResult GetScenarios()
    {
        var scenarios = new[]
        {
            new { id = "single-read", title = "1. Lấy thông tin đơn lẻ (Single Read by Id)", description = "So sánh FindAsync vs AsNoTracking vs QuerySingleOrDefaultAsync" },
            new { id = "filter-query", title = "2. Truy vấn danh sách có Lọc & Sắp xếp (Filtered Query)", description = "So sánh Where + OrderBy + Take trong LINQ vs SQL Query" },
            new { id = "join-query", title = "3. Truy vấn Multi-table Join (Product + Category)", description = "So sánh Include() trong EF Core vs Multi-mapping splitOn trong Dapper" },
            new { id = "bulk-insert", title = "4. Thêm mới hàng loạt (Bulk Insert 100 items)", description = "So sánh AddRangeAsync vs ExecuteAsync với danh sách tham số" },
            new { id = "update", title = "5. Cập nhật dữ liệu (Update Scenario)", description = "So sánh EF ChangeTracker UPDATE vs ExecuteUpdateAsync vs Explicit SQL UPDATE" }
        };

        return Ok(scenarios);
    }

    [HttpGet("run-benchmark")]
    public async Task<IActionResult> RunBenchmark(
        [FromQuery] string scenario = "single-read",
        [FromQuery] int iterations = 10,
        [FromQuery] bool isColdStart = false)
    {
        if (iterations < 1) iterations = 1;
        if (iterations > 1000) iterations = 1000;

        var result = await _benchmarkService.RunScenarioAsync(scenario, iterations, isColdStart);
        return Ok(result);
    }

    [HttpGet("flow")]
    public IActionResult GetFlowInfo()
    {
        var efFlow = _flowAnalyzer.GetEfCoreFlowInfo();
        var sqlCmdFlow = _flowAnalyzer.GetSqlCommandFlowInfo();
        var dapperFlow = _flowAnalyzer.GetDapperFlowInfo();

        return Ok(new
        {
            efCore = efFlow,
            sqlCommand = sqlCmdFlow,
            dapper = dapperFlow
        });
    }
}
