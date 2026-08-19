using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/locations")]
[Tags("Employee locations")]
public class LocationsController(CrmDbContext db) : ControllerBase
{
    [HttpPost("batch")]
    [Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> Upload(LocationBatchRequest request)
    {
        if (request.Points.Count is < 1 or > 500)
            return BadRequest(new { message = "Send between 1 and 500 location points." });

        var now = DateTime.UtcNow;
        var lowerBound = now.AddDays(-7);
        var upperBound = now.AddMinutes(10);
        var valid = request.Points
            .Where(x => x.Latitude is >= -90 and <= 90 && x.Longitude is >= -180 and <= 180)
            .Where(x => x.AccuracyMeters >= 0 && x.RecordedAtUtc >= lowerBound && x.RecordedAtUtc <= upperBound)
            .OrderBy(x => x.RecordedAtUtc)
            .ToList();
        if (valid.Count == 0) return BadRequest(new { message = "No valid location points were supplied." });

        var userId = User.UserId();
        var times = valid.Select(x => x.RecordedAtUtc).ToList();
        var existing = await db.EmployeeLocations
            .Where(x => x.UserId == userId && times.Contains(x.RecordedAtUtc))
            .Select(x => x.RecordedAtUtc).ToListAsync();
        var existingSet = existing.ToHashSet();
        var rows = valid.Where(x => !existingSet.Contains(x.RecordedAtUtc)).Select(x => new EmployeeLocation
        {
            UserId = userId, Latitude = x.Latitude, Longitude = x.Longitude,
            AccuracyMeters = x.AccuracyMeters, SpeedMetersPerSecond = x.SpeedMetersPerSecond,
            Heading = x.Heading, AltitudeMeters = x.AltitudeMeters, IsMocked = x.IsMocked,
            RecordedAtUtc = DateTime.SpecifyKind(x.RecordedAtUtc, DateTimeKind.Utc), ReceivedAtUtc = now
        }).ToList();
        db.EmployeeLocations.AddRange(rows);
        await db.SaveChangesAsync();
        return Ok(new { accepted = rows.Count });
    }

    [HttpGet("live")]
    [backend.Security.RequirePermission(PermissionCodes.LeadsManage)]
    public async Task<ActionResult> Live()
    {
        var employees = await db.Users
            .Where(x => x.IsActive && x.Role.Name == "SalesExecutive")
            .Select(x => new { x.Id, x.FullName, x.Phone })
            .OrderBy(x => x.FullName)
            .ToListAsync();
        var employeeIds = employees.Select(x => x.Id).ToList();
        var latestIds = db.EmployeeLocations.Where(x => employeeIds.Contains(x.UserId))
            .GroupBy(x => x.UserId).Select(g => g.Max(x => x.Id));
        var locations = await db.EmployeeLocations.Where(x => latestIds.Contains(x.Id)).ToListAsync();
        var byEmployee = locations.ToDictionary(x => x.UserId);
        var now = DateTime.UtcNow;

        return Ok(employees.Select(employee =>
        {
            byEmployee.TryGetValue(employee.Id, out var location);
            return new
            {
                employeeId = employee.Id, employee.FullName, employee.Phone,
                latitude = location?.Latitude, longitude = location?.Longitude,
                accuracyMeters = location?.AccuracyMeters,
                speedMetersPerSecond = location?.SpeedMetersPerSecond,
                isMocked = location?.IsMocked ?? false,
                recordedAtUtc = location?.RecordedAtUtc,
                hasLocation = location is not null,
                isOnline = location?.RecordedAtUtc >= now.AddMinutes(-5)
            };
        }));
    }

    [HttpGet("history/{employeeId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> History(int employeeId, [FromQuery] DateOnly date, [FromQuery] int timezoneOffsetMinutes = 360)
    {
        if (timezoneOffsetMinutes is < -720 or > 840) return BadRequest(new { message = "Invalid timezone offset." });
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var start = localStart.AddMinutes(-timezoneOffsetMinutes);
        var end = start.AddDays(1);
        var employee = await db.Users.Where(x => x.Id == employeeId && x.Role.Name == "SalesExecutive")
            .Select(x => new { x.Id, x.FullName, x.Phone }).FirstOrDefaultAsync();
        if (employee is null) return NotFound(new { message = "Employee not found." });
        var points = await db.EmployeeLocations.Where(x => x.UserId == employeeId && x.RecordedAtUtc >= start && x.RecordedAtUtc < end)
            .OrderBy(x => x.RecordedAtUtc)
            .Select(x => new { x.Latitude, x.Longitude, x.AccuracyMeters, x.SpeedMetersPerSecond, x.IsMocked, x.RecordedAtUtc })
            .ToListAsync();
        return Ok(new { employee, date, points, summary = Summarize(points.Select(x => (x.Latitude, x.Longitude, x.RecordedAtUtc)).ToList()) });
    }

    private static object Summarize(List<(double Latitude, double Longitude, DateTime RecordedAtUtc)> points)
    {
        double km = 0;
        for (var i = 1; i < points.Count; i++) km += Haversine(points[i - 1], points[i]);
        return new { pointCount = points.Count, distanceKm = Math.Round(km, 2),
            startedAtUtc = points.Count > 0 ? points[0].RecordedAtUtc : (DateTime?)null,
            endedAtUtc = points.Count > 0 ? points[^1].RecordedAtUtc : (DateTime?)null };
    }

    private static double Haversine((double Latitude, double Longitude, DateTime RecordedAtUtc) a, (double Latitude, double Longitude, DateTime RecordedAtUtc) b)
    {
        const double radius = 6371;
        var dLat = (b.Latitude - a.Latitude) * Math.PI / 180;
        var dLon = (b.Longitude - a.Longitude) * Math.PI / 180;
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(a.Latitude * Math.PI / 180) * Math.Cos(b.Latitude * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return radius * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }
}
