using Microsoft.EntityFrameworkCore;

namespace ProcessTracker;

// I am the bridge between the application and the SQLite database
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<BusinessProcess> Processes { get; set; }
    public DbSet<AuditLog>        AuditLogs { get; set; }
}
