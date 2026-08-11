using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
[Tags("Users")]
public class UsersController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<UserSummaryDto>>> GetUsers()
    {
        var users = await db.Users.Include(x => x.Role)
            .Select(x => new UserSummaryDto(x.Id, x.FullName, x.Email, x.Phone, x.Role.Name, x.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("/api/sales-executives")]
    [Authorize(Roles = "SuperAdmin,Admin,SubAdmin,Manager,CS,CA,VehicleDepartment")]
    public async Task<ActionResult> GetSalesExecutives()
    {
        var users = await db.Users.Include(x => x.Role)
            .Where(x => x.Role.Name == "SalesExecutive" && x.IsActive)
            .Select(x => new { x.Id, x.FullName, x.Email, x.Phone })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> CreateUser(CreateUserRequest request)
    {
        return await CreateUserInternal(request.FullName, request.Email, request.Phone, request.Role, request.Password);
    }

    [HttpPost("sales-executives")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> CreateSalesExecutive(CreateSalesExecutiveRequest request)
    {
        return await CreateUserInternal(request.FullName, request.Email, request.Phone, "SalesExecutive", request.Password);
    }

    [HttpGet("sales-executives/{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    public async Task<ActionResult> GetSalesExecutiveDetail(int id)
    {
        var user = await db.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id && x.Role.Name == "SalesExecutive");
        if (user is null) return NotFound(new { message = "Sales executive not found." });

        var leads = db.Leads.Where(x => x.AssignedToId == id);
        var activeFollowUpStatuses = new[]
        {
            LeadStatus.Contacted, LeadStatus.Interested, LeadStatus.FollowUpNeeded,
            LeadStatus.SiteVisitScheduled, LeadStatus.Visited, LeadStatus.Negotiation,
            LeadStatus.InvoiceGenerated
        };
        var approvedCollections = db.Payments.Where(x =>
            x.SalesExecutiveId == id && x.Status == PaymentStatus.Approved);

        return Ok(new
        {
            user.Id, user.FullName, user.Email, user.Phone, user.IsActive, user.CreatedAt, user.LastLoginAt,
            metrics = new
            {
                totalAssignedLeads = await leads.CountAsync(),
                returnedLeads = await db.LeadReturns.CountAsync(x => x.SalesExecutiveId == id),
                assignedStage = await leads.CountAsync(x => x.Status == LeadStatus.Assigned),
                followingUp = await leads.CountAsync(x => activeFollowUpStatuses.Contains(x.Status)),
                positiveCustomers = await db.Customers.CountAsync(x => x.AssignedToId == id && x.Lead != null && x.Lead.Status == LeadStatus.Booked),
                lost = await leads.CountAsync(x => x.Status == LeadStatus.Lost),
                notInterested = await leads.CountAsync(x => x.Status == LeadStatus.NotInterested),
                approvedCollectionCount = await approvedCollections.CountAsync(),
                approvedCollectionAmount = await approvedCollections.SumAsync(x => x.Amount),
                commission = await db.Commissions
                    .Where(x => x.SalesExecutiveId == id && x.Status != CommissionStatus.Rejected)
                    .SumAsync(x => x.Amount)
            },
            recentLeads = await leads.OrderByDescending(x => x.CreatedAt).Take(20)
                .Select(x => new
                {
                    x.Id, x.CustomerName, x.Phone, x.Status, x.ProjectId,
                    Project = x.Project == null ? null : x.Project.Name,
                    x.NextFollowUpAt, x.CreatedAt
                }).ToListAsync()
        });
    }

    [HttpPut("sales-executives/{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> UpdateSalesExecutive(int id, UpdateSalesExecutiveRequest request)
    {
        var user = await db.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id && x.Role.Name == "SalesExecutive");
        if (user is null) return NotFound(new { message = "Sales executive not found." });
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Name, email, and phone are required." });
        if (await db.Users.AnyAsync(x => x.Id != id && x.Email == request.Email.Trim()))
            return Conflict(new { message = "Email already exists." });

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim();
        user.Phone = request.Phone.Trim();
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = PasswordHash.Create(request.Password);
        await db.SaveChangesAsync();
        return NoContent();
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

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> UpdateUser(int id, UpdateAdminUserRequest request)
    {
        var user = await db.Users.FindAsync(id); if (user is null) return NotFound();
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == request.Role && x.IsActive); if (role is null) return BadRequest(new { message = "Invalid role." });
        if (await db.Users.AnyAsync(x => x.Id != id && x.Email == request.Email.Trim())) return Conflict(new { message = "Email already exists." });
        user.FullName=request.FullName.Trim(); user.Email=request.Email.Trim(); user.Phone=request.Phone.Trim(); user.RoleId=role.Id; user.IsActive=request.IsActive;
        if(!string.IsNullOrWhiteSpace(request.Password))user.PasswordHash=PasswordHash.Create(request.Password); await db.SaveChangesAsync(); return NoContent();
    }
    public record UpdateAdminUserRequest(string FullName,string Email,string Phone,string Role,bool IsActive,string? Password);
}
