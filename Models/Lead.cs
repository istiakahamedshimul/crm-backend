namespace backend.Models;

public class Lead
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? AlternativePhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BudgetRange { get; set; }
    public string? PreferredLocation { get; set; }
    public string? InterestedProject { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public LeadSource Source { get; set; } = LeadSource.ManualEntry;
    public LeadPriority Priority { get; set; } = LeadPriority.Warm;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastFollowUpAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public string? Remarks { get; set; }
}
