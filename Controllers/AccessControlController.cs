using backend.Data; using backend.Models; using backend.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace backend.Controllers;
[ApiController, Authorize(Roles="SuperAdmin"), Route("api/access-control"), Tags("Access Control")]
public class AccessControlController(CrmDbContext db) : ControllerBase
{
    [HttpGet] public async Task<ActionResult> Get() => Ok(new {
        roles = await db.Roles.Select(x => new { x.Id, x.Name, x.Department, x.IsActive, permissionIds = x.RolePermissions.Select(p => p.PermissionId) }).ToListAsync(),
        groups = await db.PermissionGroups.Select(x => new { x.Id, x.Name, x.Description, permissions = x.Permissions.Select(p => new { p.Id, p.Code, p.Name }) }).ToListAsync()
    });
    [HttpPost("groups")] public async Task<ActionResult> Group(GroupRequest r) { if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(); var x = new PermissionGroup{Name=r.Name.Trim(),Description=r.Description}; db.Add(x); await db.SaveChangesAsync(); return Ok(x); }
    [HttpPost("permissions")] public async Task<ActionResult> Permission(PermissionRequest r) { if (await db.Permissions.AnyAsync(x=>x.Code==r.Code)) return Conflict(new {message="Permission code exists."}); var x=new Permission{Code=r.Code.Trim(),Name=r.Name.Trim(),PermissionGroupId=r.GroupId};db.Add(x);await db.SaveChangesAsync();return Ok(x);}
    [HttpPost("roles")] public async Task<ActionResult> Role(RoleRequest r) { if(await db.Roles.AnyAsync(x=>x.Name==r.Name))return Conflict(new{message="Role exists."});var x=new Role{Name=r.Name.Trim(),Department=r.Department,IsActive=true};db.Add(x);await db.SaveChangesAsync();return Ok(x);}
    [HttpPut("roles/{id:int}")] public async Task<ActionResult> Role(int id, RoleRequest r) {var x=await db.Roles.FindAsync(id);if(x is null)return NotFound();x.Name=r.Name.Trim();x.Department=r.Department;x.IsActive=r.IsActive;await db.SaveChangesAsync();return NoContent();}
    [HttpPut("roles/{id:int}/permissions")] public async Task<ActionResult> RolePermissions(int id, IdsRequest r) { if(!await db.Roles.AnyAsync(x=>x.Id==id))return NotFound(); await db.RolePermissions.Where(x=>x.RoleId==id).ExecuteDeleteAsync(); foreach(var pid in r.Ids.Distinct())db.RolePermissions.Add(new RolePermission{RoleId=id,PermissionId=pid});await db.SaveChangesAsync();return NoContent();}
    [HttpPut("users/{id:int}/permissions")] public async Task<ActionResult> UserPermissions(int id, IdsRequest r) { if(!await db.Users.AnyAsync(x=>x.Id==id))return NotFound(); await db.UserPermissions.Where(x=>x.UserId==id).ExecuteDeleteAsync();foreach(var pid in r.Ids.Distinct())db.UserPermissions.Add(new UserPermission{UserId=id,PermissionId=pid});await db.SaveChangesAsync();return NoContent();}
    public record GroupRequest(string Name,string? Description); public record PermissionRequest(string Code,string Name,int GroupId); public record RoleRequest(string Name,string? Department,bool IsActive=true); public record IdsRequest(int[] Ids);
}
