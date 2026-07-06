namespace backend.Dtos;

public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, int UserId, string FullName, string Email, string Role);
