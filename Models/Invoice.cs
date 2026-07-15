namespace backend.Models;

public class Invoice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public string InvoiceNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal FinalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Generated;
    public int GeneratedById { get; set; }
    public User GeneratedBy { get; set; } = null!;
}
