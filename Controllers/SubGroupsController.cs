using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/subgroups")]
[Tags("Subgroups")]
public class SubGroupsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetSubGroups() => Ok(await db.SubGroups
        .OrderBy(x => x.Name)
        .Select(x => new { x.Id, x.Name, x.CompanyName, x.Description, ProjectCount = x.Projects.Count })
        .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateSubGroup(CreateSubGroupRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0) return BadRequest(new { message = "Subgroup name is required." });
        if (await db.SubGroups.AnyAsync(x => x.Name == name)) return Conflict(new { message = "Subgroup already exists." });
        var subgroup = new SubGroup { Name = name, Description = request.Description };
        db.SubGroups.Add(subgroup);
        await db.SaveChangesAsync();
        return Created($"/api/subgroups/{subgroup.Id}", new { subgroup.Id });
    }
}
