using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/vehicles"), Tags("Vehicles")]
public class VehiclesController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get() => Ok(await db.Vehicles.OrderBy(x => x.RegistrationNumber).ToListAsync());

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> Create(SaveVehicleRequest request)
    {
        var error = Validate(request); if (error != null) return BadRequest(new { message = error });
        var vehicle = new Vehicle(); Apply(vehicle, request); db.Vehicles.Add(vehicle);
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new { message = "Registration number already exists." }); }
        return Created($"/api/vehicles/{vehicle.Id}", vehicle);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> Update(int id, SaveVehicleRequest request)
    {
        var vehicle = await db.Vehicles.FindAsync(id); if (vehicle == null) return NotFound();
        var error = Validate(request); if (error != null) return BadRequest(new { message = error });
        Apply(vehicle, request);
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new { message = "Registration number already exists." }); }
        return Ok(vehicle);
    }

    [HttpPatch("{id:int}/status"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> Status(int id, [FromBody] bool isActive)
    { var vehicle = await db.Vehicles.FindAsync(id); if (vehicle == null) return NotFound(); vehicle.IsActive = isActive; await db.SaveChangesAsync(); return NoContent(); }

    private static string? Validate(SaveVehicleRequest x) => string.IsNullOrWhiteSpace(x.RegistrationNumber) || string.IsNullOrWhiteSpace(x.VehicleType) || string.IsNullOrWhiteSpace(x.Brand) || string.IsNullOrWhiteSpace(x.Model)
        ? "Registration number, vehicle type, brand, and model are required." : x.SeatingCapacity is < 1 or > 100 ? "Seating capacity must be between 1 and 100." : null;
    private static void Apply(Vehicle x, SaveVehicleRequest r) { x.RegistrationNumber = r.RegistrationNumber.Trim().ToUpperInvariant(); x.VehicleType = r.VehicleType.Trim(); x.Brand = r.Brand.Trim(); x.Model = r.Model.Trim(); x.Color = r.Color?.Trim(); x.SeatingCapacity = r.SeatingCapacity; x.IsActive = r.IsActive; }
}
