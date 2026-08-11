namespace backend.Models;

public class DailyWorkReport
{
    public long Id { get; set; }
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public DateOnly WorkDate { get; set; }
    public string Summary { get; set; } = "";
    public string InputLanguage { get; set; } = "bn_BD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
