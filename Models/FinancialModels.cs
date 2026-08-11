namespace backend.Models;

public class FinancialAgreement
{
    public int Id { get; set; } public int CustomerId { get; set; } public Customer Customer { get; set; } = null!;
    public decimal TotalAgreedAmount { get; set; } public decimal BookingAmount { get; set; } public PaymentPlanType PaymentPlan { get; set; }
    public DateTime? EmiStartDate { get; set; } public decimal? MonthlyEmiAmount { get; set; } public int? InstallmentCount { get; set; }
    public string? Remarks { get; set; } public int CreatedById { get; set; } public User CreatedBy { get; set; } = null!;
    public int UpdatedById { get; set; } public User UpdatedBy { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<EmiInstallment> Installments { get; set; } = [];
}
public class EmiInstallment
{
    public int Id { get; set; } public int FinancialAgreementId { get; set; } public FinancialAgreement FinancialAgreement { get; set; } = null!;
    public int InstallmentNumber { get; set; } public DateTime DueDate { get; set; } public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; } public InstallmentStatus Status { get; set; } public DateTime? PaidAt { get; set; }
}
public class FinancialAuditLog
{
    public long Id { get; set; } public int CustomerId { get; set; } public Customer Customer { get; set; } = null!; public int? PaymentId { get; set; }
    public FinancialAuditAction Action { get; set; } public string DetailsJson { get; set; } = "{}"; public int PerformedById { get; set; } public User PerformedBy { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AuditLog
{
    public long Id { get; set; } public string EntityType { get; set; } = ""; public string EntityId { get; set; } = ""; public string Action { get; set; } = ""; public string DetailsJson { get; set; } = "{}"; public int PerformedById { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class NotificationSettings { public int Id { get; set; } = 1; public int DueCheckIntervalMinutes { get; set; } = 60; public int DueSoonDays { get; set; } = 0; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; }
