namespace backend.Models;

public class AppNotification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "General";
    public string? Screen { get; set; }
    public int? LeadId { get; set; }
    public int? CustomerId { get; set; }
    public string EventKey { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public string? CustomerName { get; set; }
    public string? FileId { get; set; }
    public decimal? DueAmount { get; set; }
    public decimal? OutstandingBalance { get; set; }
    public DateTime? DueDate { get; set; }
}
