using backend.Models;

namespace backend.Dtos;

public record LeadDto(int Id, string CustomerName, string Phone, string? Email, LeadSource Source, LeadPriority Priority, LeadStatus Status, int? AssignedToId, string? AssignedToName, int? ProjectId, string? ProjectName, DateTime? NextFollowUpAt);
public record CreateLeadRequest(int? CustomerId, string CustomerName, string Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, int? ProjectId, LeadSource Source, LeadPriority Priority, int? AssignedToId, string? Remarks);
public record UpdateLeadRequest(LeadStatus? Status, LeadPriority? Priority, int? AssignedToId, DateTime? NextFollowUpAt, string? Remarks);
