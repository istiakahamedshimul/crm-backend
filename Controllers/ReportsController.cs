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
    [HttpGet("workspace")]
    public async Task<ActionResult> Workspace([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int? salesExecutiveId = null, [FromQuery] int? projectId = null)
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

        var employees = await employeeQuery.OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName, x.IsActive, Team = x.SalesTeam == null ? "Unassigned" : x.SalesTeam.Name }).ToListAsync();
        var employeeIds = employees.Select(x => x.Id).ToArray();
        var endExclusive = finish.AddDays(1);
        var monthFrom = new DateOnly(start.Year, start.Month, 1);
        var monthTo = new DateOnly(finish.Year, finish.Month, 1);

        var leadQuery = db.Leads.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < endExclusive);
        if (salesExecutiveId.HasValue) leadQuery = leadQuery.Where(x => x.AssignedToId == salesExecutiveId);
        if (projectId.HasValue) leadQuery = leadQuery.Where(x => x.ProjectId == projectId);
        var leads = await leadQuery.Select(x => new { x.Id, x.Status, x.AssignedToId, x.ProjectId, x.NextFollowUpAt }).ToListAsync();

        var outcomeQuery = db.LeadStatusHistories.AsNoTracking().Where(x => x.ChangedAt >= start && x.ChangedAt < endExclusive && employeeIds.Contains(x.ChangedById));
        if (projectId.HasValue) outcomeQuery = outcomeQuery.Where(x => x.Lead.ProjectId == projectId);
        var outcomes = await outcomeQuery.Where(x => x.ToStatus == LeadStatus.Booked || x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested)
            .Select(x => new { x.LeadId, x.ChangedById, x.ToStatus, x.Lead.ProjectId }).ToListAsync();

        var bookingQuery = db.Customers.AsNoTracking().Where(x => x.BookedAt >= start && x.BookedAt < endExclusive && x.BookedById.HasValue && employeeIds.Contains(x.BookedById.Value));
        if (projectId.HasValue) bookingQuery = bookingQuery.Where(x => x.ProjectId == projectId);
        var bookings = await bookingQuery.Select(x => new { x.Id, x.LeadId, EmployeeId = x.BookedById!.Value, x.ProjectId, Project = x.Project == null ? "No project" : x.Project.Name, AgreedValue = x.FinancialAgreement == null ? 0 : x.FinancialAgreement.TotalAgreedAmount }).ToListAsync();

        var returnQuery = db.LeadReturns.AsNoTracking().Where(x => x.ReturnedAt >= start && x.ReturnedAt < endExclusive && employeeIds.Contains(x.SalesExecutiveId));
        if (projectId.HasValue) returnQuery = returnQuery.Where(x => x.Lead.ProjectId == projectId);
        var returns = await returnQuery.OrderByDescending(x => x.ReturnedAt).Select(x => new { x.Id, x.LeadId, Lead = x.Lead.CustomerName, EmployeeId = x.SalesExecutiveId, Employee = x.SalesExecutive.FullName, Project = x.Lead.Project == null ? "No project" : x.Lead.Project.Name, x.AssignedAt, x.ReturnedAt, x.NotificationCount, Status = x.Lead.Status.ToString(), CurrentEmployee = x.Lead.AssignedTo == null ? "Unassigned" : x.Lead.AssignedTo.FullName }).ToListAsync();

        var collectionQuery = db.MonthlyCollections.AsNoTracking().Where(x => x.Month >= monthFrom && x.Month <= monthTo && employeeIds.Contains(x.SalesExecutiveId));
        var collections = await collectionQuery.Select(x => new { EmployeeId = x.SalesExecutiveId, Employee = x.SalesExecutive.FullName, x.Month, x.Amount }).ToListAsync();
        var targets = await db.MonthlySalesTargets.AsNoTracking().Where(x => x.Month >= monthFrom && x.Month <= monthTo && employeeIds.Contains(x.SalesExecutiveId)).Select(x => new { EmployeeId = x.SalesExecutiveId, x.Month, x.MinimumSalesUnits, x.MinimumCollectionAmount }).ToListAsync();
        var assigned = await db.LeadAssignmentHistories.AsNoTracking().Where(x => x.ChangedAt >= start && x.ChangedAt < endExclusive && x.ToSalesExecutiveId.HasValue && employeeIds.Contains(x.ToSalesExecutiveId.Value)).GroupBy(x => x.ToSalesExecutiveId!.Value).Select(x => new { EmployeeId = x.Key, Count = x.Select(v => v.LeadId).Distinct().Count() }).ToDictionaryAsync(x => x.EmployeeId, x => x.Count);
        var followed = await db.FollowUps.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < endExclusive && employeeIds.Contains(x.CreatedById)).GroupBy(x => x.CreatedById).Select(x => new { EmployeeId = x.Key, Count = x.Select(v => v.LeadId).Distinct().Count() }).ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        static decimal Rate(decimal value, decimal total) => total <= 0 ? 0 : Math.Round(value / total * 100, 2);
        static decimal Cap(decimal value) => Math.Min(100, Math.Max(0, value));
        var kpis = employees.Select(employee =>
        {
            var won = bookings.Where(x => x.EmployeeId == employee.Id && x.LeadId.HasValue).Select(x => x.LeadId).Distinct().Count();
            var lost = outcomes.Where(x => x.ChangedById == employee.Id && (x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested)).Select(x => x.LeadId).Distinct().Count();
            var returned = returns.Count(x => x.EmployeeId == employee.Id);
            var assignedCount = assigned.GetValueOrDefault(employee.Id);
            var units = bookings.Count(x => x.EmployeeId == employee.Id);
            var collected = collections.Where(x => x.EmployeeId == employee.Id).Sum(x => x.Amount);
            var unitTarget = targets.Where(x => x.EmployeeId == employee.Id).Sum(x => x.MinimumSalesUnits);
            var collectionTarget = targets.Where(x => x.EmployeeId == employee.Id).Sum(x => x.MinimumCollectionAmount);
            var responseScore = assignedCount == 0 ? 0 : Cap(100 - Rate(returned, assignedCount));
            var salesScore = unitTarget == 0 ? (units > 0 ? 100 : 0) : Cap(Rate(units, unitTarget));
            var collectionScore = collectionTarget == 0 ? (collected > 0 ? 100 : 0) : Cap(Rate(collected, collectionTarget));
            var winScore = Rate(won, won + lost);
            var followupScore = assignedCount == 0 ? 0 : Cap(Rate(followed.GetValueOrDefault(employee.Id), assignedCount));
            var score = Math.Round(responseScore * .30m + salesScore * .25m + collectionScore * .20m + winScore * .15m + followupScore * .10m, 2);
            var rating = score >= 85 ? "Excellent" : score >= 70 ? "Good" : score >= 50 ? "Needs improvement" : "Critical";
            return new { employeeId = employee.Id, employee = employee.FullName, employee.Team, active = employee.IsActive, score, rating, won, lost, returned, returnedRate = Rate(returned, assignedCount), collections = collected, collectionTarget, collectionAchievement = collectionScore, salesUnits = units, salesTarget = unitTarget, salesAchievement = salesScore, winRate = winScore, followupCoverage = followupScore, assignedLeads = assignedCount };
        }).OrderByDescending(x => x.score).ToList();

        var activeStatuses = new[] { LeadStatus.New, LeadStatus.Assigned, LeadStatus.Contacted, LeadStatus.Interested, LeadStatus.FollowUpNeeded, LeadStatus.SiteVisitScheduled, LeadStatus.Visited, LeadStatus.Negotiation };
        var leadSummary = new[] { new { totalLeads = leads.Count, newLeads = leads.Count(x => x.Status == LeadStatus.New), assigned = leads.Count(x => x.AssignedToId.HasValue), unassigned = leads.Count(x => !x.AssignedToId.HasValue), active = leads.Count(x => activeStatuses.Contains(x.Status)), won = bookings.Where(x => x.LeadId.HasValue).Select(x => x.LeadId).Distinct().Count(), lost = outcomes.Where(x => x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested).Select(x => x.LeadId).Distinct().Count(), returned = returns.Count, awaitingFollowUp = leads.Count(x => x.NextFollowUpAt.HasValue && x.NextFollowUpAt < endExclusive) } };
        var salesSummary = bookings.GroupBy(x => new { x.EmployeeId, x.ProjectId, x.Project }).Select(g => new { employee = employees.First(x => x.Id == g.Key.EmployeeId).FullName, project = g.Key.Project, salesUnits = g.Count(), agreedSalesValue = g.Sum(x => x.AgreedValue), won = g.Where(x => x.LeadId.HasValue).Select(x => x.LeadId).Distinct().Count(), lost = outcomes.Where(x => x.ChangedById == g.Key.EmployeeId && x.ProjectId == g.Key.ProjectId && (x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested)).Select(x => x.LeadId).Distinct().Count() }).OrderByDescending(x => x.salesUnits).ToList();
        var collectionKeys = collections.Select(x => new { x.EmployeeId, x.Month }).Concat(targets.Select(x => new { x.EmployeeId, x.Month })).Distinct().ToList();
        var collectionSummary = collectionKeys.Select(key => { var target = targets.Where(x => x.EmployeeId == key.EmployeeId && x.Month == key.Month).Sum(x => x.MinimumCollectionAmount); var amount = collections.Where(x => x.EmployeeId == key.EmployeeId && x.Month == key.Month).Sum(x => x.Amount); return new { employee = employees.First(x => x.Id == key.EmployeeId).FullName, month = key.Month, target, collected = amount, variance = amount - target, achievement = Rate(amount, target) }; }).OrderByDescending(x => x.month).ThenBy(x => x.employee).ToList();
        var projectSummary = (await db.Projects.AsNoTracking().Where(x => !projectId.HasValue || x.Id == projectId).Select(x => new { x.Id, x.Name }).ToListAsync()).Select(project => new { project = project.Name, leadsReceived = leads.Count(x => x.ProjectId == project.Id), won = bookings.Where(x => x.ProjectId == project.Id && x.LeadId.HasValue).Select(x => x.LeadId).Distinct().Count(), lost = outcomes.Where(x => x.ProjectId == project.Id && (x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested)).Select(x => x.LeadId).Distinct().Count(), salesUnits = bookings.Count(x => x.ProjectId == project.Id), collectionAmount = bookings.Where(x => x.ProjectId == project.Id).Sum(x => x.AgreedValue) }).Where(x => x.leadsReceived > 0 || x.won > 0 || x.lost > 0 || x.salesUnits > 0).OrderByDescending(x => x.salesUnits).ToList();

        return Ok(new { from = start, to = finish, generatedAt = DateTime.UtcNow, kpis, leadSummary, salesSummary, collectionSummary, returnedLeads = returns, projectSummary, formula = new { score = "Response discipline × 30% + sales achievement × 25% + collection achievement × 20% + win rate × 15% + follow-up coverage × 10%", responseDiscipline = "100 − (returned leads ÷ assigned leads × 100)", salesAchievement = "sales units ÷ sales-unit target × 100", collectionAchievement = "collections ÷ collection target × 100", winRate = "won ÷ (won + lost) × 100", followupCoverage = "distinct assigned leads followed up ÷ assigned leads × 100", note = "Component scores are capped at 100%. Returned leads have the highest individual weight." } });
    }
}
