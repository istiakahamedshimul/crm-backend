using backend.Models;

namespace backend.Dtos;

public record LeadDto(int Id, string CustomerName, string Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, LeadSource Source, string? ReferrerName, string? ReferrerPhone, string? ReferrerEmail, int? PreviousCustomerId, LeadStatus Status, int? AssignedToId, string? AssignedToName, int? ProjectId, string? ProjectName, ProjectType? ProjectType, DateTime? NextFollowUpAt, string? Remarks, DateTime? AssignedAt, DateTime CreatedAt);
public record CreateLeadRequest(int? CustomerId, string CustomerName, string Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, int? ProjectId, LeadSource Source, int? AssignedToId, string? Remarks, string? ReferrerName, string? ReferrerPhone, string? ReferrerEmail);
public record CreateMyLeadRequest(string CustomerName, string Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, int? ProjectId, LeadSource Source, string? Remarks, string? ReferrerName, string? ReferrerPhone, string? ReferrerEmail);
public record UpdateLeadRequest(LeadStatus? Status, int? AssignedToId, int? ProjectId, DateTime? NextFollowUpAt, string? Remarks, string? CustomerName, string? Phone, string? AlternativePhone, string? Email, string? Address, string? BudgetRange, string? PreferredLocation, LeadSource? Source, string? ReferrerName, string? ReferrerPhone, string? ReferrerEmail);
