using Microsoft.EntityFrameworkCore;

namespace ProcessTracker;

// I implement IProcessRepository using Entity Framework Core and SQLite
public class ProcessRepository : IProcessRepository
{
    private readonly AppDbContext _db;

    public ProcessRepository(AppDbContext db)
    {
        _db = db;
    }

    // I return every process in the database
    public async Task<List<BusinessProcess>> GetAllAsync()
        => await _db.Processes.ToListAsync();

    // I find a single process by ID — null if not found
    public async Task<BusinessProcess?> GetByIdAsync(int id)
        => await _db.Processes.FindAsync(id);

    // I create a new process with PENDING status and save it
    public async Task<BusinessProcess> CreateAsync(BusinessProcess process)
    {
        process.Status    = "PENDING";
        process.CreatedAt = DateTime.UtcNow;
        _db.Processes.Add(process);
        await _db.SaveChangesAsync();
        return process;
    }

    // I update an existing process and record the change in the audit log
    public async Task<BusinessProcess?> UpdateAsync(int id, BusinessProcess updated, string changedBy = "USER")
    {
        var p = await _db.Processes.FindAsync(id);
        if (p is null) return null;

        var oldStatus  = p.Status;
        p.Status       = updated.Status;
        p.AssignedTo   = updated.AssignedTo;

        if (updated.Status == "COMPLETED")
            p.CompletedAt = DateTime.UtcNow;

        // I write every status change to the audit log
        _db.AuditLogs.Add(new AuditLog
        {
            ProcessId = p.Id,
            OldStatus = oldStatus,
            NewStatus = updated.Status,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Reason    = "Status updated via API"
        });

        await _db.SaveChangesAsync();
        return p;
    }

    // I delete a process and return true if it existed
    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Processes.FindAsync(id);
        if (p is null) return false;
        _db.Processes.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }
}
