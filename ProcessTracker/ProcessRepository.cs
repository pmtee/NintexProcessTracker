using Microsoft.EntityFrameworkCore;
using ProcessTracker;

// I implement IProcessRepository using Entity Framework Core and SQLite
// Program.cs injects this wherever IProcessRepository is needed
public class ProcessRepository : IProcessRepository
{
    private readonly AppDbContext _db;

    // I receive AppDbContext through dependency injection
    public ProcessRepository(AppDbContext db)
    {
        _db = db;
    }

    // I return all processes from the database
    public async Task<List<BusinessProcess>> GetAllAsync()
        => await _db.Processes.ToListAsync();

    // I find a single process by ID — returns null if not found
    public async Task<BusinessProcess?> GetByIdAsync(int id)
        => await _db.Processes.FindAsync(id);

    // I create a new process, set defaults, and save to database
    public async Task<BusinessProcess> CreateAsync(BusinessProcess process)
    {
        process.CreatedAt = DateTime.UtcNow;
        process.Status = "PENDING";
        _db.Processes.Add(process);
        await _db.SaveChangesAsync();
        return process;
    }

    // I update an existing process and set completion time if COMPLETED
    public async Task<BusinessProcess?> UpdateAsync(int id, BusinessProcess updated)
    {
        var p = await _db.Processes.FindAsync(id);
        if (p is null) return null;

        p.Status = updated.Status;
        p.AssignedTo = updated.AssignedTo;

        // I record completion time when process reaches COMPLETED status
        if (updated.Status == "COMPLETED")
            p.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return p;
    }

    // I delete a process and return true if successful, false if not found
    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Processes.FindAsync(id);
        if (p is null) return false;

        _db.Processes.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }
}