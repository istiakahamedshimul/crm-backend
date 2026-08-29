namespace backend.Models;

public class LeadStatusHistory
{
    public long Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public LeadStatus FromStatus { get; set; }
    public LeadStatus ToStatus { get; set; }
    public int ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class LeadAssignmentHistory
{
    public long Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public int? FromSalesExecutiveId { get; set; }
    public int? ToSalesExecutiveId { get; set; }
    public int ChangedById { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}

public class MonthlyCollectionAudit
{
    public long Id { get; set; }
    public int MonthlyCollectionId { get; set; }
    public MonthlyCollection MonthlyCollection { get; set; } = null!;
    public decimal PreviousAmount { get; set; }
    public decimal NewAmount { get; set; }
    public string? PreviousRemarks { get; set; }
    public string? NewRemarks { get; set; }
    public int ChangedById { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class CustomerDueAudit
{
    public long Id { get; set; }
    public int CustomerDueId { get; set; }
    public CustomerDue CustomerDue { get; set; } = null!;
    public CustomerDueStatus FromStatus { get; set; }
    public CustomerDueStatus ToStatus { get; set; }
    public int ChangedById { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Remarks { get; set; }
}

public class ReportAccessAudit
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string ReportKey { get; set; } = "";
    public string Action { get; set; } = "View";
    public string FiltersJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
