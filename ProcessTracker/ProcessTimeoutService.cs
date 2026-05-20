using Microsoft.EntityFrameworkCore;

namespace ProcessTracker;

// I am a background service that runs every 60 seconds automatically.
// I find processes stuck in IN_PROGRESS for over 24 hours and mark them FAILED.
// No human intervention needed — pure automation.
public class ProcessTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly ILogger<ProcessTimeoutService>    _logger;
    private readonly TimeSpan                          _interval = TimeSpan.FromSeconds(60);
    private readonly TimeSpan                          _timeout  = TimeSpan.FromHours(24);

    public ProcessTimeoutService(
        IServiceScopeFactory           scopeFactory,
        ILogger<ProcessTimeoutService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ProcessTimeoutService started — checking every {Interval}s", _interval.TotalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckForStuckProcesses();
            }
            catch (Exception ex)
            {
                // I log errors but never crash the service
                _logger.LogError(ex, "Error in ProcessTimeoutService");
            }

            await Task.Delay(_interval, ct);
        }
    }

    private async Task CheckForStuckProcesses()
    {
        // I create a new scope because DbContext is scoped, not singleton
        using var scope  = _scopeFactory.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cutoff       = DateTime.UtcNow - _timeout;

        var stuckProcesses = await db.Processes
            .Where(p => p.Status == "IN_PROGRESS" && p.CreatedAt < cutoff)
            .ToListAsync();

        if (!stuckProcesses.Any()) return;

        foreach (var process in stuckProcesses)
        {
            var oldStatus     = process.Status;
            process.Status    = "FAILED";
            process.CompletedAt = DateTime.UtcNow;

            db.AuditLogs.Add(new AuditLog
            {
                ProcessId = process.Id,
                OldStatus = oldStatus,
                NewStatus = "FAILED",
                ChangedBy = "SYSTEM",
                ChangedAt = DateTime.UtcNow,
                Reason    = $"Auto-failed: stuck in IN_PROGRESS for over {_timeout.TotalHours} hours"
            });
        }

        await db.SaveChangesAsync();
        _logger.LogWarning("Auto-failed {Count} stalled process(es)", stuckProcesses.Count);
    }
}
