using LinkCrawler.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkCrawler.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<CrawledPage> CrawledPage { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CrawledPage>(entity =>
        {
            entity.HasIndex(p => p.Url).IsUnique();

            entity.HasIndex(p => p.Domain);
        });
    }


}
