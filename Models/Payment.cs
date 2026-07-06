namespace backend.Models;

public class Payment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? ProofUrl { get; set; }
    public string? Remarks { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public int? SubmittedById { get; set; }
    public User? SubmittedBy { get; set; }
    public int? VerifiedById { get; set; }
    public User? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? GatewayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
