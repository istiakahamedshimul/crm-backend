namespace backend.Dtos;

public record CreateCustomerFromLeadRequest(string? Occupation, string? NidOrPassport, string? NomineeName, string? NomineePhone);
public record CreateCustomerRequest(int LeadId, string? Name, string? Phone, string? AlternativePhone, string? Email, string? Address, string? Occupation, string? NidOrPassport, string? NomineeName, string? NomineePhone);
public record UpdateCustomerProjectRequest(int? ProjectId);
