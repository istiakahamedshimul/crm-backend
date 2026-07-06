using backend.Models;

namespace backend.Dtos;

public record LeadDto(int Id, string CustomerName, string Phone, string? Email, LeadSource Source, LeadPriority Priority, LeadStatus Status, int? AssignedToId, string? AssignedToName, DateTime? NextFollowUpAt);
public record CreateLeadRequest(string CustomerName, string Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, string? InterestedProject, LeadSource Source, LeadPriority Priority, int? AssignedToId, string? Remarks);
public record UpdateLeadRequest(LeadStatus? Status, LeadPriority? Priority, int? AssignedToId, DateTime? NextFollowUpAt, string? Remarks);
