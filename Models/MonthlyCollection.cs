namespace backend.Models;

public class MonthlyCollection
{
    public int Id { get; set; }
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public DateOnly Month { get; set; }
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
    public int RecordedById { get; set; }
    public User RecordedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
