namespace backend.Models;

public class CustomerDue
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateOnly Month { get; set; }
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
    public CustomerDueStatus Status { get; set; } = CustomerDueStatus.Unpaid;
    public DateTime DueDate { get; set; }
    public int? PaidById { get; set; }
    public User? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidRemarks { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public int RecordedById { get; set; }
    public User RecordedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
