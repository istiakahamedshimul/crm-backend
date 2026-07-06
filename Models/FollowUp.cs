namespace backend.Models;

public class FollowUp
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public FollowUpType Type { get; set; }
    public string Summary { get; set; } = "";
    public string? CustomerResponse { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<FollowUpProof> Proofs { get; set; } = [];
}
