using backend.Data;
using backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
[Tags("Profile")]
public class ProfileController(CrmDbContext db) : ControllerBase
{
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
}
