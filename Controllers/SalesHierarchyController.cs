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
        var userId = User.UserId();
        var groupRows = db.SalesGroups.AsNoTracking().Include(x => x.GroupLeader).AsQueryable();
        if (User.IsInRole("GroupLeader")) groupRows = groupRows.Where(x => x.GroupLeaderId == userId);
        else if (User.IsInRole("SalesExecutive")) groupRows = groupRows.Where(x => db.SalesTeams.Any(t => t.SalesGroupId == x.Id && t.TeamLeaderId == userId));
        else if (!User.IsInRole("SuperAdmin") && !User.IsInRole("BrandAndIT")) return Forbid();
        var groups = await groupRows.OrderBy(x => x.Name).ToListAsync();
        var groupIds = groups.Select(x => x.Id).ToList();
        var teams = await db.SalesTeams.AsNoTracking().Include(x => x.TeamLeader).Where(x => groupIds.Contains(x.SalesGroupId)).OrderBy(x => x.ParentTeamId).ThenBy(x => x.Name).ToListAsync();
        if (User.IsInRole("SalesExecutive")) { var ledIds = teams.Where(x => x.TeamLeaderId == userId).Select(x => x.Id).ToHashSet(); teams = teams.Where(x => ledIds.Contains(x.Id) || x.ParentTeamId.HasValue && ledIds.Contains(x.ParentTeamId.Value)).ToList(); }
        var teamIds = teams.Select(x => x.Id).ToList();
        var members = await db.Users.AsNoTracking().Where(x => x.SalesTeamId.HasValue && teamIds.Contains(x.SalesTeamId.Value)).OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName, x.Designation, x.IsActive, x.SalesTeamId }).ToListAsync();
        return Ok(groups.Select(g => new { g.Id, g.Name, g.GroupLeaderId, GroupLeader = g.GroupLeader.FullName, g.IsActive, Teams = teams.Where(t => t.SalesGroupId == g.Id).Select(t => new { t.Id, t.Name, t.ParentTeamId, t.TeamLeaderId, TeamLeader = t.TeamLeader?.FullName, t.IsActive, MemberCount = members.Count(u => u.SalesTeamId == t.Id && u.IsActive), Members = members.Where(u => u.SalesTeamId == t.Id).Select(u => new { u.Id, u.FullName, u.Designation, u.IsActive }) }) }));
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

    [HttpGet("groups/{groupId:int}/report"), Authorize(Roles = "SuperAdmin,GroupLeader,SalesExecutive")]
    public async Task<ActionResult> GroupReport(int groupId, [FromQuery] DateOnly month)
    {
        if (!await CanViewGroup(groupId)) return Forbid();
        month = new DateOnly(month.Year, month.Month, 1);
        var group = await db.SalesGroups.Where(x => x.Id == groupId).Select(x => new { x.Id, x.Name, Leader = x.GroupLeader.FullName }).SingleOrDefaultAsync();
        if (group is null) return NotFound();
        var target = await db.SalesGroupTargets.Where(x => x.SalesGroupId == groupId && x.Month == month).Select(x => new { x.UnitTarget, x.CollectionTarget }).SingleOrDefaultAsync();
        var teamQuery = db.SalesTeams.Where(x => x.SalesGroupId == groupId);
        if (User.IsInRole("SalesExecutive")) { var userId = User.UserId(); teamQuery = teamQuery.Where(x => x.TeamLeaderId == userId || x.ParentTeam != null && x.ParentTeam.TeamLeaderId == userId); }
        var teamRows = await teamQuery.AsNoTracking().Include(x => x.TeamLeader).ToListAsync();
        var teamIds = teamRows.Select(x => x.Id).ToList();
        var memberRows = await db.Users.AsNoTracking().Where(x => x.SalesTeamId.HasValue && teamIds.Contains(x.SalesTeamId.Value)).Select(x => new { x.Id, x.FullName, x.Designation, x.SalesTeamId }).ToListAsync();
        var memberIds = memberRows.Select(x => x.Id).ToList();
        var start = month.ToDateTime(TimeOnly.MinValue); var end = start.AddMonths(1);
        var wins = await db.Customers.Where(x => x.BookedById.HasValue && memberIds.Contains(x.BookedById.Value) && x.BookedAt >= start && x.BookedAt < end).GroupBy(x => x.BookedById!.Value).Select(x => new { UserId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count);
        var collections = await db.MonthlyCollections.Where(x => memberIds.Contains(x.SalesExecutiveId) && x.Month == month).GroupBy(x => x.SalesExecutiveId).Select(x => new { UserId = x.Key, Amount = x.Sum(y => y.Amount) }).ToDictionaryAsync(x => x.UserId, x => x.Amount);
        var teamTargets = await db.SalesTeamTargets.Where(x => teamIds.Contains(x.SalesTeamId) && x.Month == month).ToDictionaryAsync(x => x.SalesTeamId);
        var teams = teamRows.Select(t => new { t.Id, t.Name, t.ParentTeamId, Leader = t.TeamLeader?.FullName, Target = teamTargets.TryGetValue(t.Id, out var tt) ? new { tt.UnitTarget, tt.CollectionTarget } : null, Members = memberRows.Where(u => u.SalesTeamId == t.Id).Select(u => new { u.Id, u.FullName, u.Designation, Units = wins.GetValueOrDefault(u.Id), Collection = collections.GetValueOrDefault(u.Id) }).ToList() }).ToList();
        return Ok(new { group, month, target, teams, totals = new { units = wins.Values.Sum(), collection = collections.Values.Sum() } });
    }

    private async Task<bool> CanManageGroup(int groupId) => User.IsInRole("SuperAdmin") || await db.SalesGroups.AnyAsync(x => x.Id == groupId && x.GroupLeaderId == User.UserId());
    private async Task<bool> CanViewGroup(int groupId) => await CanManageGroup(groupId) || User.IsInRole("SalesExecutive") && await db.SalesTeams.AnyAsync(x => x.SalesGroupId == groupId && x.TeamLeaderId == User.UserId());
    private void Set(SalesGroupTarget row, TargetRequest request) { row.UnitTarget = request.UnitTarget; row.CollectionTarget = request.CollectionTarget; row.UpdatedById = User.UserId(); row.UpdatedAt = DateTime.UtcNow; }
    public record GroupRequest(string Name, int GroupLeaderId);
    public record TeamRequest(string Name, int SalesGroupId, int? ParentTeamId, int? TeamLeaderId);
    public record TargetRequest(DateOnly Month, int UnitTarget, decimal CollectionTarget);
    public record TeamLeaderRequest(int? TeamLeaderId);
}
