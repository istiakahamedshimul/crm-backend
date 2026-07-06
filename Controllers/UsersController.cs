using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
[Route("api/users")]
[Tags("Users")]
public class UsersController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserSummaryDto>>> GetUsers()
    {
        var users = await db.Users.Include(x => x.Role)
            .Select(x => new UserSummaryDto(x.Id, x.FullName, x.Email, x.Phone, x.Role.Name, x.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("/api/sales-executives")]
    public async Task<ActionResult> GetSalesExecutives()
    {
        var users = await db.Users.Include(x => x.Role)
            .Where(x => x.Role.Name == "SalesExecutive" && x.IsActive)
            .Select(x => new { x.Id, x.FullName, x.Email, x.Phone })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateUser(CreateUserRequest request)
    {
        return await CreateUserInternal(request.FullName, request.Email, request.Phone, request.Role, request.Password);
    }

    [HttpPost("sales-executives")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateSalesExecutive(CreateSalesExecutiveRequest request)
    {
        return await CreateUserInternal(request.FullName, request.Email, request.Phone, "SalesExecutive", request.Password);
    }

    private async Task<ActionResult> CreateUserInternal(string fullName, string email, string phone, string roleName, string password)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
        if (role is null) return BadRequest(new { message = "Invalid role." });
        if (await db.Users.AnyAsync(x => x.Email == email)) return Conflict(new { message = "Email already exists." });

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            RoleId = role.Id,
            PasswordHash = PasswordHash.Create(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Created($"/api/users/{user.Id}", new { user.Id });
    }
}
