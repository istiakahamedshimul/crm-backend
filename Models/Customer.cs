namespace backend.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? AlternativePhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NidOrPassport { get; set; }
    public string? Occupation { get; set; }
    public string? NomineeName { get; set; }
    public string? NomineePhone { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public string DocumentStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
