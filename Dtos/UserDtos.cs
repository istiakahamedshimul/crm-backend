namespace backend.Dtos;

public record CreateUserRequest(string FullName, string Email, string Phone, string Role, string Password);
public record CreateSalesExecutiveRequest(string FullName, string Email, string Phone, string Password);
public record UserSummaryDto(int Id, string FullName, string Email, string Phone, string Role, bool IsActive);
