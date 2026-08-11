namespace backend.Models;

public class Payment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string CollectionNumber { get; set; } = "";
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentPurpose? Purpose { get; set; }
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
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? TransactionReference { get; set; }
    public int? InstallmentId { get; set; }
    public EmiInstallment? Installment { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public int? ReversedById { get; set; }
    public User? ReversedBy { get; set; }
    public string? ReversalReason { get; set; }
    public string? IdempotencyKey { get; set; }
}
