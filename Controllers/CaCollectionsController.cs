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
    public async Task<ActionResult> Collections() => Ok(await db.MonthlyCollections.AsNoTracking().OrderByDescending(x => x.Month).ThenBy(x => x.SalesExecutive.FullName).Select(x => new { x.Id, x.SalesExecutiveId, SalesExecutive = x.SalesExecutive.FullName, Team = x.SalesExecutive.SalesTeam == null ? null : x.SalesExecutive.SalesTeam.Name, Group = x.SalesExecutive.SalesTeam == null ? null : x.SalesExecutive.SalesTeam.SalesGroup.Name, x.Month, x.Amount, x.Remarks, RecordedBy = x.RecordedBy.FullName, x.CreatedAt, x.UpdatedAt }).ToListAsync());

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
        else db.MonthlyCollectionAudits.Add(new MonthlyCollectionAudit { MonthlyCollectionId = row.Id, PreviousAmount = row.Amount, NewAmount = request.Amount, PreviousRemarks = row.Remarks, NewRemarks = request.Remarks?.Trim(), ChangedById = User.UserId() });
        row.Amount = request.Amount; row.Remarks = request.Remarks?.Trim(); row.RecordedById = User.UserId();
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { row.Id, row.SalesExecutiveId, SalesExecutive = employee.FullName, row.Month, row.Amount, row.Remarks });
    }

    [HttpGet("customer-dues"), RequirePermission(PermissionCodes.PaymentsView)]
    public async Task<ActionResult> Dues() => Ok(await db.CustomerDues.AsNoTracking().OrderBy(x => x.Status).ThenByDescending(x => x.DueDate).Select(x => new { x.Id, x.CustomerId, x.Customer.FileId, Customer = x.Customer.Name, Project = x.Customer.Project == null ? null : x.Customer.Project.Name, x.Month, x.DueDate, x.Amount, x.Status, x.Remarks, SalesExecutive = x.Customer.AssignedTo == null ? null : x.Customer.AssignedTo.FullName, PaidBy = x.PaidBy == null ? null : x.PaidBy.FullName, x.PaidAt, x.PaidRemarks, x.NotificationSentAt, x.CreatedAt }).ToListAsync());

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
        if (row is null) { row = new CustomerDue { CustomerId = customer.Id, Month = month, DueDate = request.DueDate?.Date ?? month.ToDateTime(TimeOnly.MinValue), RecordedById = User.UserId() }; db.CustomerDues.Add(row); }
        else if (row.Status != CustomerDueStatus.Unpaid) return Conflict(new { message = "A paid or cancelled due cannot be overwritten. Create a new due or reopen it with an audit reason." });
        row.Amount = request.Amount; row.Remarks = request.Remarks?.Trim(); row.RecordedById = User.UserId();
        await db.SaveChangesAsync();
        var title = isNew ? "Customer due added" : "Customer due updated";
        var message = $"File {customer.FileId}: {row.Amount:0.00} due for {month:MMMM yyyy}.";
        db.AppNotifications.Add(new AppNotification { UserId = customer.AssignedToId.Value, CustomerId = customer.Id, CustomerName = customer.Name, FileId = customer.FileId, DueAmount = row.Amount, DueDate = month.ToDateTime(TimeOnly.MinValue), Title = title, Message = message, Type = "CustomerDue", Screen = "customer", EventKey = $"ca-due:{row.Id}:{DateTime.UtcNow.Ticks}" });
        await db.SaveChangesAsync();
        await push.SendFinancialPushAsync(customer.AssignedToId.Value, customer.Id, title, message, HttpContext.RequestAborted);
        row.NotificationSentAt = DateTime.UtcNow; await db.SaveChangesAsync();
        return Ok(new { row.Id, row.CustomerId, customer.FileId, row.Month, row.DueDate, row.Amount, row.Status, row.Remarks });
    }

    [HttpPost("customer-dues/{id:int}/paid"), RequirePermission(PermissionCodes.PaymentsRecord)]
    public async Task<ActionResult> MarkDuePaid(int id, DueDecisionRequest request)
    {
        var row = await db.CustomerDues.Include(x => x.Customer).SingleOrDefaultAsync(x => x.Id == id);
        if (row is null) return NotFound();
        if (row.Status == CustomerDueStatus.Paid) return Ok(new { row.Id, row.Status, row.PaidAt });
        var previous = row.Status; row.Status = CustomerDueStatus.Paid; row.PaidAt = DateTime.UtcNow; row.PaidById = User.UserId(); row.PaidRemarks = request.Remarks?.Trim();
        db.CustomerDueAudits.Add(new CustomerDueAudit { CustomerDueId = row.Id, FromStatus = previous, ToStatus = row.Status, ChangedById = User.UserId(), Remarks = row.PaidRemarks });
        if (row.Customer.AssignedToId.HasValue) db.AppNotifications.Add(new AppNotification { UserId = row.Customer.AssignedToId.Value, CustomerId = row.CustomerId, CustomerName = row.Customer.Name, FileId = row.Customer.FileId, DueAmount = row.Amount, DueDate = row.DueDate, Title = "Customer due marked paid", Message = $"File {row.Customer.FileId}: {row.Amount:0.00} due has been marked paid by CA.", Type = "CustomerDuePaid", Screen = "customer", EventKey = $"ca-due-paid:{row.Id}" });
        await db.SaveChangesAsync();
        return Ok(new { row.Id, row.Status, row.PaidAt, row.PaidRemarks });
    }

    [HttpPost("customer-dues/{id:int}/reopen"), RequirePermission(PermissionCodes.PaymentsRecord)]
    public async Task<ActionResult> ReopenDue(int id, DueDecisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Remarks)) return BadRequest(new { message = "An audit reason is required to reopen a due." });
        var row = await db.CustomerDues.SingleOrDefaultAsync(x => x.Id == id); if (row is null) return NotFound();
        var previous = row.Status; row.Status = CustomerDueStatus.Unpaid; row.PaidAt = null; row.PaidById = null; row.PaidRemarks = null;
        db.CustomerDueAudits.Add(new CustomerDueAudit { CustomerDueId = row.Id, FromStatus = previous, ToStatus = row.Status, ChangedById = User.UserId(), Remarks = request.Remarks.Trim() });
        await db.SaveChangesAsync(); return Ok(new { row.Id, row.Status });
    }

    public record MonthlyCollectionRequest(int SalesExecutiveId, DateOnly Month, decimal Amount, string? Remarks);
    public record CustomerDueRequest(string FileId, DateOnly Month, decimal Amount, string? Remarks, DateTime? DueDate = null);
    public record DueDecisionRequest(string? Remarks);
}
