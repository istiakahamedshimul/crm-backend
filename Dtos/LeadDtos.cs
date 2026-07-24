using backend.Models;

namespace backend.Dtos;

public record LeadDto(int Id, string CustomerName, string Phone, string? Email, LeadSource Source, LeadStatus Status, int? AssignedToId, string? AssignedToName, int? ProjectId, string? ProjectName, ProjectType? ProjectType, DateTime? NextFollowUpAt);
public record CreateLeadRequest(int? CustomerId, string CustomerName, string Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, int? ProjectId, LeadSource Source, int? AssignedToId, string? Remarks);
public record UpdateLeadRequest(LeadStatus? Status, int? AssignedToId, int? ProjectId, DateTime? NextFollowUpAt, string? Remarks);
