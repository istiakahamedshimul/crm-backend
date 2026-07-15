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
[Route("api/vehicle-bookings")]
[Tags("Vehicle Bookings")]
public class VehicleBookingsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetBookings()
    {
        var query = db.VehicleBookings.Include(x => x.SalesExecutive).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.SalesExecutiveId == User.UserId());

        return Ok(await query.OrderByDescending(x => x.VisitDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.SalesExecutiveId,
                SalesExecutive = x.SalesExecutive.FullName,
                x.VisitDate,
                x.PersonCount,
                x.VisitPlace,
                x.PickupPlace,
                x.Status,
                x.AdminRemarks,
                x.CreatedAt
            }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> CreateBooking(CreateVehicleBookingRequest request)
    {
        if (request.PersonCount is < 1 or > 50)
            return BadRequest(new { message = "Person count must be between 1 and 50." });
        if (string.IsNullOrWhiteSpace(request.VisitPlace) || string.IsNullOrWhiteSpace(request.PickupPlace))
            return BadRequest(new { message = "Visit place and pickup place are required." });
        if (request.TimezoneOffsetMinutes is < -840 or > 840)
            return BadRequest(new { message = "Invalid timezone offset." });

        var localNow = DateTime.UtcNow.AddMinutes(request.TimezoneOffsetMinutes);
        var submittedLocal = DateTime.SpecifyKind(request.ClientLocalDateTime, DateTimeKind.Unspecified);
        if (Math.Abs((submittedLocal - localNow).TotalMinutes) > 15)
            return BadRequest(new { message = "Your device date or time is incorrect. Enable automatic date and time and try again." });

        var today = DateOnly.FromDateTime(localNow);
        var tomorrow = today.AddDays(1);
        if (request.VisitDate <= today)
            return BadRequest(new { message = "Vehicle booking must be for a future date." });
        if (request.VisitDate == tomorrow && localNow.TimeOfDay >= new TimeSpan(19, 0, 0))
            return BadRequest(new { message = "Next-day vehicle booking closes at 7:00 PM local time. Please select a later date." });

        var booking = new VehicleBooking
        {
            SalesExecutiveId = User.UserId(),
            VisitDate = request.VisitDate,
            PersonCount = request.PersonCount,
            VisitPlace = request.VisitPlace.Trim(),
            PickupPlace = request.PickupPlace.Trim(),
            ClientLocalDateTime = submittedLocal,
            TimezoneOffsetMinutes = request.TimezoneOffsetMinutes
        };
        db.VehicleBookings.Add(booking);
        await db.SaveChangesAsync();
        return Created($"/api/vehicle-bookings/{booking.Id}", new { booking.Id, booking.Status });
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public Task<ActionResult> Approve(int id, ReviewVehicleBookingRequest request) =>
        Review(id, VehicleBookingStatus.Approved, request.Remarks);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public Task<ActionResult> Reject(int id, ReviewVehicleBookingRequest request) =>
        Review(id, VehicleBookingStatus.Rejected, request.Remarks);

    private async Task<ActionResult> Review(int id, VehicleBookingStatus status, string? remarks)
    {
        var booking = await db.VehicleBookings.FindAsync(id);
        if (booking is null) return NotFound(new { message = "Vehicle booking not found." });
        booking.Status = status;
        booking.AdminRemarks = remarks;
        booking.ReviewedById = User.UserId();
        booking.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = $"Vehicle booking {status.ToString().ToLowerInvariant()}." });
    }
}
