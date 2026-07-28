namespace backend.Models;

public class LeadAutomationSettings
{
    public int Id { get; set; } = 1;
    public decimal UnassignAfterHours { get; set; } = 24;
    public decimal ReminderIntervalHours { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
