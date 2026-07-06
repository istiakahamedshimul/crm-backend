using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/units")]
[Tags("Property Units")]
public class UnitsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetUnits([FromQuery] int? projectId, [FromQuery] UnitStatus? status)
    {
        var query = db.PropertyUnits.Include(x => x.Project).AsQueryable();
        if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId);
        if (status.HasValue) query = query.Where(x => x.Status == status);

        var units = await query.Select(x => new
        {
            x.Id,
            Project = x.Project.Name,
            x.TowerOrBlock,
            x.FloorNumber,
            x.UnitNumber,
            x.SizeSqft,
            x.FinalPrice,
            x.BookingMoney,
            x.Status
        }).ToListAsync();

        return Ok(units);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateUnit(CreateUnitRequest request)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == request.ProjectId))
        {
            return BadRequest(new { message = "Project not found." });
        }

        var unit = new PropertyUnit
        {
            ProjectId = request.ProjectId,
            TowerOrBlock = request.TowerOrBlock,
            FloorNumber = request.FloorNumber,
            UnitNumber = request.UnitNumber,
            SizeSqft = request.SizeSqft,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            FacingDirection = request.FacingDirection,
            BasePrice = request.BasePrice,
            FinalPrice = request.FinalPrice,
            BookingMoney = request.BookingMoney,
            Status = UnitStatus.Available
        };

        db.PropertyUnits.Add(unit);
        await db.SaveChangesAsync();
        return Created($"/api/units/{unit.Id}", new { unit.Id });
    }
}
