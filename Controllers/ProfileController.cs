using backend.Data;
using backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
[Tags("Profile")]
public class ProfileController(CrmDbContext db) : ControllerBase
{
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpGet]
    public async Task<ActionResult> GetProfile()
    {
        var userId = User.UserId();
        var user = await db.Users.Include(x => x.Role)
            .Where(x => x.Id == userId)
            .Select(x => new { x.Id, x.FullName, x.Email, Role = x.Role.Name, x.Phone })
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(new { message = "The new password must contain at least 8 characters." });

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == User.UserId());
        if (user is null) return NotFound();
        if (!PasswordHash.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "The current password is incorrect." });

        user.PasswordHash = PasswordHash.Create(request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
