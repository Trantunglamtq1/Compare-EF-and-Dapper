using System.Data;
using CompareEfAndDapper.Web.Data;
using CompareEfAndDapper.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure DB Provider (SQL Server with fallback to SQLite)
string sqlServerConn = builder.Configuration.GetConnectionString("SqlServerDocker") 
                       ?? builder.Configuration.GetConnectionString("SqlServer") 
                       ?? "Server=localhost;Database=EfVsDapperDb;Trusted_Connection=True;TrustServerCertificate=True;";
string sqliteConn = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=ef_vs_dapper.db";

bool preferSqlServer = builder.Configuration.GetValue<bool>("UseSqlServer", true);
bool isSqlServerAvailable = false;

if (preferSqlServer)
{
    try
    {
        using var testConn = new SqlConnection(sqlServerConn);
        testConn.Open();
        isSqlServerAvailable = true;
    }
    catch
    {
        isSqlServerAvailable = false;
    }
}

if (isSqlServerAvailable)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(sqlServerConn);
        options.LogTo(AppDbContext.LogSql, Microsoft.Extensions.Logging.LogLevel.Information);
    });

    builder.Services.AddTransient<Func<IDbConnection>>(_ => () => new SqlConnection(sqlServerConn));
}
else
{
    // SQLite Fallback so the app runs out-of-the-box seamlessly
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlite(sqliteConn);
        options.LogTo(AppDbContext.LogSql, Microsoft.Extensions.Logging.LogLevel.Information);
    });

    builder.Services.AddTransient<Func<IDbConnection>>(_ => () => new SqliteConnection(sqliteConn));
}

builder.Services.AddScoped<DapperRepository>();
builder.Services.AddSingleton<ExecutionFlowAnalyzer>();
builder.Services.AddScoped<PerformanceBenchmarkService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

// Initialize database schema and sample data
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
}

app.Run();
