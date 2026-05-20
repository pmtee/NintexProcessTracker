namespace ProcessTracker;

// I record every status change — who changed it, when, and why
public class AuditLog
{
    public int      Id        { get; set; }
    public int      ProcessId { get; set; }
    public string   OldStatus { get; set; } = string.Empty;
    public string   NewStatus { get; set; } = string.Empty;
    public string   ChangedBy { get; set; } = "USER";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string   Reason    { get; set; } = string.Empty;
}
