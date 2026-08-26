namespace backend.Dtos;

public record CreateUserRequest(string FullName, string Email, string Phone, string Role, string Password);
public record CreateSalesExecutiveRequest(string FullName, string Email, string Phone, string Designation, string Password, int? SalesTeamId = null);
public record UpdateSalesExecutiveRequest(string FullName, string Email, string Phone, string Designation, bool IsActive, string? Password, int MinimumSalesUnits, decimal MinimumCollectionAmount, DateOnly? TargetMonth, int? SalesTeamId = null);
public record UserSummaryDto(int Id, string FullName, string Email, string Phone, string? Designation, string Role, bool IsActive, int? SalesTeamId = null, string? SalesTeam = null, string? SalesGroup = null);
