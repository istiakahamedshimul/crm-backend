using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/ca"), Tags("CA Collections and Dues")]
public class CaCollectionsController(CrmDbContext db, IOneSignalNotificationService push) : ControllerBase
{
    [HttpGet("monthly-collections"), RequirePermission(PermissionCodes.PaymentsView)]
    public async Task<ActionResult> Collections() => Ok(await db.MonthlyCollections.AsNoTracking().OrderByDescending(x => x.Month).ThenBy(x => x.SalesExecutive.FullName).Select(x => new { x.Id, x.SalesExecutiveId, SalesExecutive = x.SalesExecutive.FullName, x.Month, x.Amount, x.Remarks, x.CreatedAt }).ToListAsync());

    [HttpGet("monthly-collections/mine"), Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> MyCollections() => Ok(await db.MonthlyCollections.AsNoTracking().Where(x => x.SalesExecutiveId == User.UserId()).OrderByDescending(x => x.Month).Select(x => new { x.Id, x.Month, x.Amount, x.Remarks, x.CreatedAt }).ToListAsync());

    [HttpPost("monthly-collections"), RequirePermission(PermissionCodes.PaymentsRecord)]
    public async Task<ActionResult> SaveCollection(MonthlyCollectionRequest request)
    {
        if (request.Amount < 0) return BadRequest(new { message = "Collection amount cannot be negative." });
        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);
        var employee = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == request.SalesExecutiveId && x.Role.Name == "SalesExecutive" && x.IsActive);
        if (employee is null) return BadRequest(new { message = "Active sales employee not found." });
        var row = await db.MonthlyCollections.SingleOrDefaultAsync(x => x.SalesExecutiveId == request.SalesExecutiveId && x.Month == month);
        if (row is null) { row = new MonthlyCollection { SalesExecutiveId = request.SalesExecutiveId, Month = month, RecordedById = User.UserId() }; db.MonthlyCollections.Add(row); }
        row.Amount = request.Amount; row.Remarks = request.Remarks?.Trim(); row.RecordedById = User.UserId();
        await db.SaveChangesAsync();
        return Ok(new { row.Id, row.SalesExecutiveId, SalesExecutive = employee.FullName, row.Month, row.Amount, row.Remarks });
    }

    [HttpGet("customer-dues"), RequirePermission(PermissionCodes.PaymentsView)]
    public async Task<ActionResult> Dues() => Ok(await db.CustomerDues.AsNoTracking().OrderByDescending(x => x.Month).ThenBy(x => x.Customer.FileId).Select(x => new { x.Id, x.CustomerId, x.Customer.FileId, x.Month, x.Amount, x.Remarks, SalesExecutive = x.Customer.AssignedTo == null ? null : x.Customer.AssignedTo.FullName, x.CreatedAt }).ToListAsync());

    [HttpPost("customer-dues"), RequirePermission(PermissionCodes.PaymentsRecord)]
    public async Task<ActionResult> SaveDue(CustomerDueRequest request)
    {
        if (request.Amount <= 0) return BadRequest(new { message = "Due amount must be greater than zero." });
        var fileId = request.FileId?.Trim();
        if (string.IsNullOrWhiteSpace(fileId)) return BadRequest(new { message = "Customer file number is required." });
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.FileId == fileId);
        if (customer is null) return BadRequest(new { message = "No customer was found with that file number." });
        if (customer.AssignedToId is null) return BadRequest(new { message = "The customer has no assigned sales employee." });
        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);
        var row = await db.CustomerDues.SingleOrDefaultAsync(x => x.CustomerId == customer.Id && x.Month == month);
        var isNew = row is null;
        if (row is null) { row = new CustomerDue { CustomerId = customer.Id, Month = month, RecordedById = User.UserId() }; db.CustomerDues.Add(row); }
        row.Amount = request.Amount; row.Remarks = request.Remarks?.Trim(); row.RecordedById = User.UserId();
        await db.SaveChangesAsync();
        var title = isNew ? "Customer due added" : "Customer due updated";
        var message = $"File {customer.FileId}: {row.Amount:0.00} due for {month:MMMM yyyy}.";
        db.AppNotifications.Add(new AppNotification { UserId = customer.AssignedToId.Value, CustomerId = customer.Id, CustomerName = customer.Name, FileId = customer.FileId, DueAmount = row.Amount, DueDate = month.ToDateTime(TimeOnly.MinValue), Title = title, Message = message, Type = "CustomerDue", Screen = "customer", EventKey = $"ca-due:{row.Id}:{DateTime.UtcNow.Ticks}" });
        await db.SaveChangesAsync();
        await push.SendFinancialPushAsync(customer.AssignedToId.Value, customer.Id, title, message, HttpContext.RequestAborted);
        return Ok(new { row.Id, row.CustomerId, customer.FileId, row.Month, row.Amount, row.Remarks });
    }

    public record MonthlyCollectionRequest(int SalesExecutiveId, DateOnly Month, decimal Amount, string? Remarks);
    public record CustomerDueRequest(string FileId, DateOnly Month, decimal Amount, string? Remarks);
}
