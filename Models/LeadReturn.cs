namespace backend.Models;

public class LeadReturn
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public DateTime ReturnedAt { get; set; } = DateTime.UtcNow;
    public int NotificationCount { get; set; }
}
