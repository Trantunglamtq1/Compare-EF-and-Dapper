using CompareEfAndDapper.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompareEfAndDapper.Web.Data;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    // Field to store captured SQL command for diagnostic inspection
    public static string? LastExecutedSql { get; set; }
    public static List<string> SqlLogHistory { get; } = new();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Price).HasPrecision(18, 2);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.Sku).IsUnique();
        });
    }

    public static void ClearLogs()
    {
        LastExecutedSql = null;
        lock (SqlLogHistory)
        {
            SqlLogHistory.Clear();
        }
    }

    public static void LogSql(string sql)
    {
        LastExecutedSql = sql;
        lock (SqlLogHistory)
        {
            SqlLogHistory.Add($"[{DateTime.Now:HH:mm:ss.fff}] {sql}");
        }
    }
}
