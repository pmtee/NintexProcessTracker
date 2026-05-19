namespace ProcessTracker;

// I define BusinessProcess as my core data model
// It represents a single business process moving through a lifecycle
public class BusinessProcess
{
    // I use Id as the primary key — EF Core auto-increments this
    public int Id { get; set; }

    // I store the process name e.g. "Server Onboarding"
    public string Name { get; set; } = "";

    // I track where the process is: PENDING, IN_PROGRESS, COMPLETED, FAILED
    public string Status { get; set; } = "PENDING";

    // I record which team member owns this process
    public string AssignedTo { get; set; } = "";

    // I automatically capture when this process was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // I only set this when the process finishes
    // It is nullable because incomplete processes have no end time
    public DateTime? CompletedAt { get; set; }
}