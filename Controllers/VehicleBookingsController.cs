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
        var query = db.VehicleBookings.Include(x => x.SalesExecutive).Include(x => x.Customer)
            .Include(x => x.Project).Include(x => x.Vehicle).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.SalesExecutiveId == User.UserId());

        return Ok(await query.OrderByDescending(x => x.VisitDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.SalesExecutiveId,
                SalesExecutive = x.SalesExecutive.FullName,
                x.CustomerId,
                Customer = x.Customer.Name,
                CustomerPhone = x.Customer.Phone,
                x.ProjectId,
                Project = x.Project.Name,
                x.VisitDate,
                x.VisitTime,
                x.PersonCount,
                x.PickupPlace,
                x.Purpose,
                x.AdditionalInformation,
                x.VehicleId,
                Vehicle = x.Vehicle == null ? null : x.Vehicle.RegistrationNumber,
                x.Driver,
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
        if (string.IsNullOrWhiteSpace(request.PickupPlace) || string.IsNullOrWhiteSpace(request.Purpose))
            return BadRequest(new { message = "Pickup location and purpose are required." });
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer is null || customer.AssignedToId != User.UserId()) return BadRequest(new { message = "Select one of your customers." });
        if (!await db.Projects.AnyAsync(x => x.Id == request.ProjectId)) return BadRequest(new { message = "Project not found." });
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
            CustomerId = request.CustomerId,
            ProjectId = request.ProjectId,
            VisitDate = request.VisitDate,
            VisitTime = request.VisitTime,
            PersonCount = request.PersonCount,
            VisitPlace = "",
            PickupPlace = request.PickupPlace.Trim(),
            Purpose = request.Purpose.Trim(),
            AdditionalInformation = request.AdditionalInformation?.Trim(),
            ClientLocalDateTime = submittedLocal,
            TimezoneOffsetMinutes = request.TimezoneOffsetMinutes
        };
        db.VehicleBookings.Add(booking);
        await db.SaveChangesAsync();
        return Created($"/api/vehicle-bookings/{booking.Id}", new { booking.Id, booking.Status });
    }

    [HttpPost("admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateAdmin(CreateAdminVehicleBookingRequest request)
    {
        var customer = await db.Customers.FindAsync(request.CustomerId);
        var vehicle = await db.Vehicles.FindAsync(request.VehicleId);
        if (customer is null || customer.AssignedToId is null) return BadRequest(new { message = "Customer must have an assigned sales employee." });
        if (!await db.Projects.AnyAsync(x => x.Id == request.ProjectId)) return BadRequest(new { message = "Project not found." });
        if (vehicle is null || !vehicle.IsActive) return BadRequest(new { message = "Select an active vehicle." });
        if (request.PersonCount < 1 || request.PersonCount > vehicle.SeatingCapacity) return BadRequest(new { message = "Visitor count exceeds vehicle capacity." });
        if (request.VisitDate < DateOnly.FromDateTime(DateTime.Today) || string.IsNullOrWhiteSpace(request.PickupPlace)) return BadRequest(new { message = "Enter a valid visit date and pickup location." });
        var booking = new VehicleBooking { SalesExecutiveId = customer.AssignedToId.Value, CustomerId = request.CustomerId, ProjectId = request.ProjectId,
            VisitDate = request.VisitDate, VisitTime = request.VisitTime, PersonCount = request.PersonCount, PickupPlace = request.PickupPlace.Trim(),
            Purpose = request.Purpose.Trim(), AdditionalInformation = request.AdditionalInformation?.Trim(), VehicleId = request.VehicleId,
            Driver = request.Driver?.Trim(), Status = VehicleBookingStatus.Approved, AdminRemarks = request.Remarks?.Trim(), ReviewedById = User.UserId(), ReviewedAt = DateTime.UtcNow };
        db.VehicleBookings.Add(booking); await db.SaveChangesAsync();
        return Created($"/api/vehicle-bookings/{booking.Id}", new { booking.Id, booking.Status });
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public Task<ActionResult> Approve(int id, ReviewVehicleBookingRequest request) =>
        Review(id, VehicleBookingStatus.Approved, request);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public Task<ActionResult> Reject(int id, ReviewVehicleBookingRequest request) =>
        Review(id, VehicleBookingStatus.Rejected, request);

    private async Task<ActionResult> Review(int id, VehicleBookingStatus status, ReviewVehicleBookingRequest request)
    {
        var booking = await db.VehicleBookings.FindAsync(id);
        if (booking is null) return NotFound(new { message = "Vehicle booking not found." });
        if (booking.Status != VehicleBookingStatus.Pending) return Conflict(new { message = "Only pending requests can be reviewed." });
        if (status == VehicleBookingStatus.Approved)
        {
            if (request.VehicleId is null) return BadRequest(new { message = "Select a vehicle before approval." });
            var vehicle = await db.Vehicles.FindAsync(request.VehicleId);
            if (vehicle is null || !vehicle.IsActive) return BadRequest(new { message = "Select an active vehicle." });
            if (booking.PersonCount > vehicle.SeatingCapacity) return BadRequest(new { message = "Visitor count exceeds vehicle capacity." });
            booking.VehicleId = vehicle.Id; booking.Driver = request.Driver?.Trim();
        }
        booking.Status = status;
        booking.AdminRemarks = request.Remarks?.Trim();
        booking.ReviewedById = User.UserId();
        booking.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = $"Vehicle booking {status.ToString().ToLowerInvariant()}." });
    }
}
