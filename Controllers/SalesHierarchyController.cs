using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/sales-hierarchy"), Tags("Sales Hierarchy")]
public class SalesHierarchyController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var groups = db.SalesGroups.AsNoTracking().AsQueryable();
        if (User.IsInRole("GroupLeader")) groups = groups.Where(x => x.GroupLeaderId == User.UserId());
        else if (!User.IsInRole("SuperAdmin") && !User.IsInRole("BrandAndIT")) return Forbid();
        return Ok(await groups.OrderBy(x => x.Name).Select(g => new
        {
            g.Id, g.Name, g.GroupLeaderId, GroupLeader = g.GroupLeader.FullName, g.IsActive,
            Teams = db.SalesTeams.Where(t => t.SalesGroupId == g.Id).OrderBy(t => t.ParentTeamId).ThenBy(t => t.Name).Select(t => new
            {
                t.Id, t.Name, t.ParentTeamId, t.TeamLeaderId, TeamLeader = t.TeamLeader == null ? null : t.TeamLeader.FullName, t.IsActive,
                MemberCount = db.Users.Count(u => u.SalesTeamId == t.Id && u.IsActive),
                Members = db.Users.Where(u => u.SalesTeamId == t.Id).OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName, u.Designation, u.IsActive })
            })
        }).ToListAsync());
    }

    [HttpPost("groups"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> CreateGroup(GroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Group name is required." });
        var leader = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == request.GroupLeaderId && x.Role.Name == "GroupLeader" && x.IsActive);
        if (leader is null) return BadRequest(new { message = "Select an active Group Leader account." });
        if (await db.SalesGroups.AnyAsync(x => x.Name == request.Name.Trim() || x.GroupLeaderId == request.GroupLeaderId)) return Conflict(new { message = "Group name or leader is already assigned." });
        var group = new SalesGroup { Name = request.Name.Trim(), GroupLeaderId = request.GroupLeaderId };
        db.Add(group); await db.SaveChangesAsync(); return Ok(new { group.Id });
    }

    [HttpPost("teams"), Authorize(Roles = "SuperAdmin,GroupLeader")]
    public async Task<ActionResult> CreateTeam(TeamRequest request)
    {
        if (!await CanManageGroup(request.SalesGroupId)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Team name is required." });
        if (request.ParentTeamId.HasValue && !await db.SalesTeams.AnyAsync(x => x.Id == request.ParentTeamId && x.SalesGroupId == request.SalesGroupId && x.ParentTeamId == null)) return BadRequest(new { message = "Parent team must belong to this group." });
        if (request.TeamLeaderId.HasValue && !await db.Users.AnyAsync(x => x.Id == request.TeamLeaderId && x.Role.Name == "SalesExecutive" && x.IsActive && (!User.IsInRole("GroupLeader") || x.SalesTeam == null || x.SalesTeam.SalesGroupId == request.SalesGroupId))) return BadRequest(new { message = "Team leader must be an available Sales Executive in this group." });
        var team = new SalesTeam { Name = request.Name.Trim(), SalesGroupId = request.SalesGroupId, ParentTeamId = request.ParentTeamId, TeamLeaderId = request.TeamLeaderId };
        db.Add(team); await db.SaveChangesAsync();
        if (request.TeamLeaderId.HasValue) { var leader = await db.Users.FindAsync(request.TeamLeaderId.Value); leader!.SalesTeamId = team.Id; await db.SaveChangesAsync(); }
        return Ok(new { team.Id });
    }

    [HttpPut("groups/{groupId:int}/target"), Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> GroupTarget(int groupId, TargetRequest request)
    {
        if (request.UnitTarget < 0 || request.CollectionTarget < 0) return BadRequest(new { message = "Targets cannot be negative." });
        if (!await db.SalesGroups.AnyAsync(x => x.Id == groupId)) return NotFound();
        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);
        var row = await db.SalesGroupTargets.SingleOrDefaultAsync(x => x.SalesGroupId == groupId && x.Month == month);
        if (row is null) { row = new SalesGroupTarget { SalesGroupId = groupId, Month = month }; db.Add(row); }
        Set(row, request); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("teams/{teamId:int}/target"), Authorize(Roles = "SuperAdmin,GroupLeader")]
    public async Task<ActionResult> TeamTarget(int teamId, TargetRequest request)
    {
        if (request.UnitTarget < 0 || request.CollectionTarget < 0) return BadRequest(new { message = "Targets cannot be negative." });
        var team = await db.SalesTeams.FindAsync(teamId); if (team is null) return NotFound();
        if (!await CanManageGroup(team.SalesGroupId)) return Forbid();
        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);
        var row = await db.SalesTeamTargets.SingleOrDefaultAsync(x => x.SalesTeamId == teamId && x.Month == month);
        if (row is null) { row = new SalesTeamTarget { SalesTeamId = teamId, Month = month }; db.Add(row); }
        row.UnitTarget = request.UnitTarget; row.CollectionTarget = request.CollectionTarget; row.UpdatedById = User.UserId(); row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("teams/{teamId:int}/leader"), Authorize(Roles = "SuperAdmin,GroupLeader")]
    public async Task<ActionResult> TeamLeader(int teamId, TeamLeaderRequest request)
    {
        var team = await db.SalesTeams.FindAsync(teamId); if (team is null) return NotFound();
        if (!await CanManageGroup(team.SalesGroupId)) return Forbid();
        if (request.TeamLeaderId.HasValue && !await db.Users.AnyAsync(x => x.Id == request.TeamLeaderId && x.Role.Name == "SalesExecutive" && x.IsActive && x.SalesTeamId == teamId)) return BadRequest(new { message = "Choose an active member of this team." });
        team.TeamLeaderId = request.TeamLeaderId; await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("groups/{groupId:int}/report"), Authorize(Roles = "SuperAdmin,GroupLeader")]
    public async Task<ActionResult> GroupReport(int groupId, [FromQuery] DateOnly month)
    {
        if (!await CanManageGroup(groupId)) return Forbid();
        month = new DateOnly(month.Year, month.Month, 1);
        var group = await db.SalesGroups.Where(x => x.Id == groupId).Select(x => new { x.Id, x.Name, Leader = x.GroupLeader.FullName }).SingleOrDefaultAsync();
        if (group is null) return NotFound();
        var target = await db.SalesGroupTargets.Where(x => x.SalesGroupId == groupId && x.Month == month).Select(x => new { x.UnitTarget, x.CollectionTarget }).SingleOrDefaultAsync();
        var teams = await db.SalesTeams.Where(x => x.SalesGroupId == groupId).Select(t => new
        {
            t.Id, t.Name, t.ParentTeamId, Leader = t.TeamLeader == null ? null : t.TeamLeader.FullName,
            Target = db.SalesTeamTargets.Where(x => x.SalesTeamId == t.Id && x.Month == month).Select(x => new { x.UnitTarget, x.CollectionTarget }).FirstOrDefault(),
            Members = db.Users.Where(u => u.SalesTeamId == t.Id).Select(u => new
            {
                u.Id, u.FullName, u.Designation,
                Units = db.Customers.Count(c => c.BookedById == u.Id && c.BookedAt >= month.ToDateTime(TimeOnly.MinValue) && c.BookedAt < month.ToDateTime(TimeOnly.MinValue).AddMonths(1)),
                Collection = db.MonthlyCollections.Where(c => c.SalesExecutiveId == u.Id && c.Month == month).Sum(c => (decimal?)c.Amount) ?? 0
            })
        }).ToListAsync();
        return Ok(new { group, month, target, teams, totals = new { units = teams.Sum(t => t.Members.Sum(m => m.Units)), collection = teams.Sum(t => t.Members.Sum(m => m.Collection)) } });
    }

    private async Task<bool> CanManageGroup(int groupId) => User.IsInRole("SuperAdmin") || await db.SalesGroups.AnyAsync(x => x.Id == groupId && x.GroupLeaderId == User.UserId());
    private void Set(SalesGroupTarget row, TargetRequest request) { row.UnitTarget = request.UnitTarget; row.CollectionTarget = request.CollectionTarget; row.UpdatedById = User.UserId(); row.UpdatedAt = DateTime.UtcNow; }
    public record GroupRequest(string Name, int GroupLeaderId);
    public record TeamRequest(string Name, int SalesGroupId, int? ParentTeamId, int? TeamLeaderId);
    public record TargetRequest(DateOnly Month, int UnitTarget, decimal CollectionTarget);
    public record TeamLeaderRequest(int? TeamLeaderId);
}
