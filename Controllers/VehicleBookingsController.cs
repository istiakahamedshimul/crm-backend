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
        var query = db.VehicleBookings.Include(x => x.SalesExecutive).Include(x => x.Customer).Include(x => x.Lead)
            .Include(x => x.Project).Include(x => x.Vehicle).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.SalesExecutiveId == User.UserId());

        return Ok(await query.OrderByDescending(x => x.VisitDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.SalesExecutiveId,
                SalesExecutive = x.SalesExecutive.FullName,
                x.CustomerId,
                x.LeadId,
                Customer = x.Lead != null
                    ? x.Lead.CustomerName
                    : x.Customer == null ? "Legacy booking" : x.Customer.Name,
                CustomerPhone = x.Lead != null
                    ? x.Lead.Phone
                    : x.Customer == null ? null : x.Customer.Phone,
                x.ProjectId,
                Project = x.Project == null ? "Legacy booking" : x.Project.Name,
                x.VisitDate,
                x.VisitTime,
                x.PersonCount,
                x.PickupPlace,
                x.Purpose,
                x.AdditionalInformation,
                x.VehicleId,
                Vehicle = x.Vehicle == null ? null : x.Vehicle.RegistrationNumber,
                x.Driver,
                x.DriverPhone,
                x.Status,
                x.AdminRemarks,
                x.CancellationReason,
                x.CancelledAt,
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
        var lead = await db.Leads.FindAsync(request.LeadId);
        if (lead is null || lead.AssignedToId != User.UserId())
            return BadRequest(new { message = "Select one of your assigned pipeline leads." });
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
            LeadId = request.LeadId,
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
    [backend.Security.RequirePermission(PermissionCodes.TransportationManage)]
    public async Task<ActionResult> CreateAdmin(CreateAdminVehicleBookingRequest request)
    {
        var customer = request.CustomerId.HasValue ? await db.Customers.FindAsync(request.CustomerId.Value) : null;
        var lead = request.LeadId.HasValue ? await db.Leads.FindAsync(request.LeadId.Value) : null;
        var vehicle = await db.Vehicles.FindAsync(request.VehicleId);
        if ((customer is null) == (lead is null)) return BadRequest(new { message = "Select either one customer or one lead." });
        var salesExecutiveId = customer?.AssignedToId ?? lead?.AssignedToId;
        if (salesExecutiveId is null) return BadRequest(new { message = "The selected customer or lead must have an assigned sales employee." });
        if (!await db.Projects.AnyAsync(x => x.Id == request.ProjectId)) return BadRequest(new { message = "Project not found." });
        if (vehicle is null || !vehicle.IsActive) return BadRequest(new { message = "Select an active vehicle." });
        // Capacity is informational; admins may assign any active vehicle.
        // if (request.PersonCount < 1 || request.PersonCount > vehicle.SeatingCapacity) return BadRequest(new { message = "Visitor count exceeds vehicle capacity." });
        if (request.VisitDate < DateOnly.FromDateTime(DateTime.Today) || string.IsNullOrWhiteSpace(request.PickupPlace)) return BadRequest(new { message = "Enter a valid visit date and pickup location." });
        var booking = new VehicleBooking { SalesExecutiveId = salesExecutiveId.Value, CustomerId = customer?.Id, LeadId = lead?.Id, ProjectId = request.ProjectId,
            VisitDate = request.VisitDate, VisitTime = request.VisitTime, PersonCount = request.PersonCount, PickupPlace = request.PickupPlace.Trim(),
            Purpose = request.Purpose.Trim(), AdditionalInformation = request.AdditionalInformation?.Trim(), VehicleId = request.VehicleId,
            Driver = request.Driver?.Trim(), DriverPhone = request.DriverPhone?.Trim(), Status = VehicleBookingStatus.Approved, AdminRemarks = request.Remarks?.Trim(), ReviewedById = User.UserId(), ReviewedAt = DateTime.UtcNow };
        db.VehicleBookings.Add(booking); await db.SaveChangesAsync();
        return Created($"/api/vehicle-bookings/{booking.Id}", new { booking.Id, booking.Status });
    }

    [HttpPost("{id:int}/approve")]
    [backend.Security.RequirePermission(PermissionCodes.TransportationManage)]
    public Task<ActionResult> Approve(int id, ReviewVehicleBookingRequest request) =>
        Review(id, VehicleBookingStatus.Approved, request);

    [HttpPost("{id:int}/reject")]
    [backend.Security.RequirePermission(PermissionCodes.TransportationManage)]
    public Task<ActionResult> Reject(int id, ReviewVehicleBookingRequest request) =>
        Review(id, VehicleBookingStatus.Rejected, request);

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> Cancel(int id, CancelVehicleBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "Cancellation reason is required." });
        var booking = await db.VehicleBookings.SingleOrDefaultAsync(x => x.Id == id && x.SalesExecutiveId == User.UserId());
        if (booking is null) return NotFound(new { message = "Visit request not found." });
        if (booking.Status is not (VehicleBookingStatus.Pending or VehicleBookingStatus.Approved)) return Conflict(new { message = "Only pending or approved visits can be cancelled." });
        var offset = booking.TimezoneOffsetMinutes == 0 ? 360 : booking.TimezoneOffsetMinutes;
        var localToday = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(offset));
        if (localToday > booking.VisitDate) return Conflict(new { message = "This visit date has passed and can no longer be cancelled." });
        booking.Status = VehicleBookingStatus.Cancelled;
        booking.CancellationReason = request.Reason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = "Visit cancelled." });
    }

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
            // Capacity is informational; admins may assign any active vehicle.
            // if (booking.PersonCount > vehicle.SeatingCapacity) return BadRequest(new { message = "Visitor count exceeds vehicle capacity." });
            booking.VehicleId = vehicle.Id; booking.Driver = request.Driver?.Trim(); booking.DriverPhone = request.DriverPhone?.Trim();
        }
        booking.Status = status;
        booking.AdminRemarks = request.Remarks?.Trim();
        booking.ReviewedById = User.UserId();
        booking.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = $"Vehicle booking {status.ToString().ToLowerInvariant()}." });
    }
}
