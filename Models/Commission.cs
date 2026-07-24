namespace backend.Models;

public class Commission
{
    public int Id { get; set; }
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public int CustomerId { get; set; }
    public int? InvoiceId { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public decimal PaymentAmount { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
