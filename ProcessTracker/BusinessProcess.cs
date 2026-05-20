using System.ComponentModel.DataAnnotations;

namespace ProcessTracker;

// I represent a single business process tracked by the system
public class BusinessProcess
{
    public int      Id          { get; set; }

    [Required]
    public string   Name        { get; set; } = string.Empty;

    public string   Status      { get; set; } = "PENDING";
    public string   AssignedTo  { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
