using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/reports"), Tags("Reports")]
[backend.Security.RequirePermission(PermissionCodes.ReportsView)]
public class ReportsController(CrmDbContext db) : ControllerBase
{
    [HttpGet("employee-kpis")]
    public async Task<ActionResult> EmployeeKpis([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int? salesExecutiveId = null)
    {
        var today = DateTime.UtcNow.Date;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var finish = (to ?? today).Date;
        if (start > finish) return BadRequest(new { message = "Start date cannot be after end date." });
        if ((finish - start).TotalDays > 3660) return BadRequest(new { message = "Report period cannot exceed ten years." });
        if (User.IsInRole("SalesExecutive")) salesExecutiveId = backend.Extensions.ClaimsPrincipalExtensions.UserId(User);

        var employeeQuery = db.Users.AsNoTracking().Where(x => x.Role.Name == "SalesExecutive");
        if (User.IsInRole("GroupLeader"))
        {
            var groupId = await db.SalesGroups.Where(x => x.GroupLeaderId == backend.Extensions.ClaimsPrincipalExtensions.UserId(User)).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            if (!groupId.HasValue) return Forbid();
            employeeQuery = employeeQuery.Where(x => x.SalesTeam != null && x.SalesTeam.SalesGroupId == groupId);
        }
        if (salesExecutiveId.HasValue) employeeQuery = employeeQuery.Where(x => x.Id == salesExecutiveId.Value);

        var employees = await employeeQuery.OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName, x.IsActive }).ToListAsync();
        var employeeIds = employees.Select(x => x.Id).ToArray();
        var endExclusive = finish.AddDays(1);
        var monthFrom = new DateOnly(start.Year, start.Month, 1);
        var monthTo = new DateOnly(finish.Year, finish.Month, 1);

        var won = await db.LeadStatusHistories.AsNoTracking()
            .Where(x => x.ChangedAt >= start && x.ChangedAt < endExclusive && x.ToStatus == LeadStatus.Booked && x.Lead.AssignedToId.HasValue && employeeIds.Contains(x.Lead.AssignedToId.Value))
            .GroupBy(x => x.Lead.AssignedToId!.Value).Select(x => new { EmployeeId = x.Key, Count = x.Select(v => v.LeadId).Distinct().Count() }).ToDictionaryAsync(x => x.EmployeeId, x => x.Count);
        var lost = await db.LeadStatusHistories.AsNoTracking()
            .Where(x => x.ChangedAt >= start && x.ChangedAt < endExclusive && (x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested) && x.Lead.AssignedToId.HasValue && employeeIds.Contains(x.Lead.AssignedToId.Value))
            .GroupBy(x => x.Lead.AssignedToId!.Value).Select(x => new { EmployeeId = x.Key, Count = x.Select(v => v.LeadId).Distinct().Count() }).ToDictionaryAsync(x => x.EmployeeId, x => x.Count);
        var returned = await db.LeadReturns.AsNoTracking()
            .Where(x => x.ReturnedAt >= start && x.ReturnedAt < endExclusive && employeeIds.Contains(x.SalesExecutiveId))
            .GroupBy(x => x.SalesExecutiveId).Select(x => new { EmployeeId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.EmployeeId, x => x.Count);
        var collections = await db.MonthlyCollections.AsNoTracking()
            .Where(x => x.Month >= monthFrom && x.Month <= monthTo && employeeIds.Contains(x.SalesExecutiveId))
            .GroupBy(x => x.SalesExecutiveId).Select(x => new { EmployeeId = x.Key, Amount = x.Sum(v => v.Amount) }).ToDictionaryAsync(x => x.EmployeeId, x => x.Amount);
        var salesUnits = await db.Customers.AsNoTracking()
            .Where(x => x.BookedAt >= start && x.BookedAt < endExclusive && x.BookedById.HasValue && employeeIds.Contains(x.BookedById.Value))
            .GroupBy(x => x.BookedById!.Value).Select(x => new { EmployeeId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        var rows = employees.Select(x => new { employeeId = x.Id, employee = x.FullName, active = x.IsActive, won = won.GetValueOrDefault(x.Id), lost = lost.GetValueOrDefault(x.Id), returned = returned.GetValueOrDefault(x.Id), collections = collections.GetValueOrDefault(x.Id), salesUnits = salesUnits.GetValueOrDefault(x.Id) }).ToList();
        return Ok(new { from = start, to = finish, generatedAt = DateTime.UtcNow, rows, definitions = new { won = "Distinct leads moved to Booked during the selected period.", lost = "Distinct leads moved to Lost or Not Interested during the selected period.", returned = "Leads returned from the employee after the response deadline; this is the primary service KPI.", collections = "Employee monthly collection totals for every month touched by the selected period.", salesUnits = "Customer bookings credited to the employee during the selected period." } });
    }
}
