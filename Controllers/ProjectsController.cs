using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
[Tags("Projects")]
public class ProjectsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetProjects()
    {
        var projects = await db.Projects.Include(x => x.SubGroup)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SubGroupId,
                SubGroup = x.SubGroup.Name,
                x.SubGroup.CompanyName,
                x.Type,
                x.Location,
                x.Status
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateProject(CreateProjectRequest request)
    {
        if (!await db.SubGroups.AnyAsync(x => x.Id == request.SubGroupId))
            return BadRequest(new { message = "Subgroup not found." });

        var project = new Project
        {
            Name = request.Name,
            SubGroupId = request.SubGroupId,
            Type = request.Type,
            Location = request.Location,
            Address = request.Address,
            Status = request.Status,
            Description = request.Description
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return Created($"/api/projects/{project.Id}", new { project.Id });
    }
}
