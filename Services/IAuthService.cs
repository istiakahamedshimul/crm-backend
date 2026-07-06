using backend.Dtos;
using backend.Models;

namespace backend.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(string email, string password);
    string CreateToken(User user);
}
