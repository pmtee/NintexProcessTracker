using Microsoft.EntityFrameworkCore;

namespace ProcessTracker;

// I use AppDbContext as my bridge between C# and the SQLite database
// Entity Framework Core uses this to create and query my tables
public class AppDbContext : DbContext
{
    // I accept database options injected from Program.cs
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // I expose a Processes table — EF Core maps this to
    // a "Processes" table in my ProcessTracker.db file
    public DbSet<BusinessProcess> Processes { get; set; }
}