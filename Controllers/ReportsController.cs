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
        if ((finish - start).TotalDays > 36525) return BadRequest(new { message = "Report period cannot exceed one hundred years." });
        if (User.IsInRole("SalesExecutive")) salesExecutiveId = backend.Extensions.ClaimsPrincipalExtensions.UserId(User);

        var employeeQuery = db.Users.AsNoTracking().Where(x => x.Role.Name == "SalesExecutive");
        if (User.IsInRole("GroupLeader"))
        {
            var groupId = await db.SalesGroups.Where(x => x.GroupLeaderId == backend.Extensions.ClaimsPrincipalExtensions.UserId(User)).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            if (!groupId.HasValue) return Forbid();
            employeeQuery = employeeQuery.Where(x => x.SalesTeam != null && x.SalesTeam.SalesGroupId == groupId);
        }
        if (salesExecutiveId.HasValue) employeeQuery = employeeQuery.Where(x => x.Id == salesExecutiveId.Value);

        var employees = await employeeQuery.OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName, x.IsActive, TeamId = x.SalesTeamId, Team = x.SalesTeam == null ? "Unassigned" : x.SalesTeam.Name, GroupId = x.SalesTeam == null ? (int?)null : x.SalesTeam.SalesGroupId, Group = x.SalesTeam == null ? "Unassigned" : x.SalesTeam.SalesGroup.Name }).ToListAsync();
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
            .Select(x => new { x.LeadId, x.ChangedById, x.ToStatus, x.Lead.ProjectId, x.ChangedAt }).ToListAsync();

        var bookingQuery = db.Customers.AsNoTracking().Where(x => x.BookedAt >= start && x.BookedAt < endExclusive && x.BookedById.HasValue && employeeIds.Contains(x.BookedById.Value));
        if (projectId.HasValue) bookingQuery = bookingQuery.Where(x => x.ProjectId == projectId);
        var bookings = await bookingQuery.Select(x => new { x.Id, x.LeadId, EmployeeId = x.BookedById!.Value, x.ProjectId, Project = x.Project == null ? "No project" : x.Project.Name, AgreedValue = x.FinancialAgreement == null ? 0 : x.FinancialAgreement.TotalAgreedAmount, BookedAt = x.BookedAt!.Value }).ToListAsync();

        var returnQuery = db.LeadReturns.AsNoTracking().Where(x => x.ReturnedAt >= start && x.ReturnedAt < endExclusive && employeeIds.Contains(x.SalesExecutiveId));
        if (projectId.HasValue) returnQuery = returnQuery.Where(x => x.Lead.ProjectId == projectId);
        var returns = await returnQuery.OrderByDescending(x => x.ReturnedAt).Select(x => new { x.Id, x.LeadId, Lead = x.Lead.CustomerName, EmployeeId = x.SalesExecutiveId, Employee = x.SalesExecutive.FullName, Project = x.Lead.Project == null ? "No project" : x.Lead.Project.Name, x.AssignedAt, x.ReturnedAt, x.NotificationCount, Status = x.Lead.Status.ToString(), CurrentEmployee = x.Lead.AssignedTo == null ? "Unassigned" : x.Lead.AssignedTo.FullName }).ToListAsync();

        var collectionQuery = db.MonthlyCollections.AsNoTracking().Where(x => x.Month >= monthFrom && x.Month <= monthTo && employeeIds.Contains(x.SalesExecutiveId));
        var collections = await collectionQuery.Select(x => new { EmployeeId = x.SalesExecutiveId, Employee = x.SalesExecutive.FullName, x.Month, x.Amount }).ToListAsync();
        var targets = await db.MonthlySalesTargets.AsNoTracking().Where(x => x.Month >= monthFrom && x.Month <= monthTo && employeeIds.Contains(x.SalesExecutiveId)).Select(x => new { EmployeeId = x.SalesExecutiveId, x.Month, x.MinimumSalesUnits, x.MinimumCollectionAmount }).ToListAsync();
        var assignmentRows = await db.LeadAssignmentHistories.AsNoTracking().Where(x => x.ChangedAt >= start && x.ChangedAt < endExclusive && x.ToSalesExecutiveId.HasValue && employeeIds.Contains(x.ToSalesExecutiveId.Value)).Select(x => new { EmployeeId = x.ToSalesExecutiveId!.Value, x.LeadId, x.ChangedAt }).ToListAsync();
        var followRows = await db.FollowUps.AsNoTracking().Where(x => x.CreatedAt >= start && x.CreatedAt < endExclusive && employeeIds.Contains(x.CreatedById)).Select(x => new { EmployeeId = x.CreatedById, x.LeadId, x.CreatedAt }).ToListAsync();
        var assigned = assignmentRows.GroupBy(x => x.EmployeeId).ToDictionary(x => x.Key, x => x.Select(v => v.LeadId).Distinct().Count());
        var followed = followRows.GroupBy(x => x.EmployeeId).ToDictionary(x => x.Key, x => x.Select(v => v.LeadId).Distinct().Count());
        var projectPayments = await db.Payments.AsNoTracking().Where(x => x.PaymentDate >= start && x.PaymentDate < endExclusive && x.Status == PaymentStatus.Approved && !x.IsReversed && employeeIds.Contains(x.SalesExecutiveId) && x.Customer.ProjectId.HasValue && (!projectId.HasValue || x.Customer.ProjectId == projectId)).GroupBy(x => x.Customer.ProjectId!.Value).Select(x => new { ProjectId = x.Key, Amount = x.Sum(v => v.Amount) }).ToDictionaryAsync(x => x.ProjectId, x => x.Amount);

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
        var projectSummary = (await db.Projects.AsNoTracking().Where(x => !projectId.HasValue || x.Id == projectId).Select(x => new { x.Id, x.Name }).ToListAsync()).Select(project => new { project = project.Name, leadsReceived = leads.Count(x => x.ProjectId == project.Id), won = bookings.Where(x => x.ProjectId == project.Id && x.LeadId.HasValue).Select(x => x.LeadId).Distinct().Count(), lost = outcomes.Where(x => x.ProjectId == project.Id && (x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested)).Select(x => x.LeadId).Distinct().Count(), salesUnits = bookings.Count(x => x.ProjectId == project.Id), agreedSalesValue = bookings.Where(x => x.ProjectId == project.Id).Sum(x => x.AgreedValue), totalCollections = projectPayments.GetValueOrDefault(project.Id) }).Where(x => x.leadsReceived > 0 || x.won > 0 || x.lost > 0 || x.salesUnits > 0 || x.totalCollections > 0).OrderByDescending(x => x.salesUnits).ToList();

        var teamSummary = employees.Where(x => x.TeamId.HasValue).GroupBy(x => new { x.TeamId, x.Team, x.Group }).Select(g => { var ids = g.Select(x => x.Id).ToHashSet(); var rows = kpis.Where(x => ids.Contains(x.employeeId)).ToList(); return new { group = g.Key.Group, team = g.Key.Team, employees = g.Count(), averageKpi = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.score), 2), won = rows.Sum(x => x.won), lost = rows.Sum(x => x.lost), returned = rows.Sum(x => x.returned), salesUnits = rows.Sum(x => x.salesUnits), collections = rows.Sum(x => x.collections) }; }).OrderByDescending(x => x.averageKpi).ToList();
        var groupSummary = employees.Where(x => x.GroupId.HasValue).GroupBy(x => new { x.GroupId, x.Group }).Select(g => { var ids = g.Select(x => x.Id).ToHashSet(); var rows = kpis.Where(x => ids.Contains(x.employeeId)).ToList(); return new { group = g.Key.Group, employees = g.Count(), teams = g.Where(x => x.TeamId.HasValue).Select(x => x.TeamId).Distinct().Count(), averageKpi = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.score), 2), won = rows.Sum(x => x.won), lost = rows.Sum(x => x.lost), returned = rows.Sum(x => x.returned), salesUnits = rows.Sum(x => x.salesUnits), collections = rows.Sum(x => x.collections) }; }).OrderByDescending(x => x.averageKpi).ToList();

        var months = new List<DateOnly>(); for (var month = monthFrom; month <= monthTo; month = month.AddMonths(1)) months.Add(month);
        var employeeMonthly = employees.SelectMany(employee => months.Select(month =>
        {
            var monthStart = month.ToDateTime(TimeOnly.MinValue); var monthEnd = month.AddMonths(1).ToDateTime(TimeOnly.MinValue);
            var monthWon = bookings.Where(x => x.EmployeeId == employee.Id && x.BookedAt >= monthStart && x.BookedAt < monthEnd && x.LeadId.HasValue).Select(x => x.LeadId).Distinct().Count();
            var monthLost = outcomes.Where(x => x.ChangedById == employee.Id && x.ChangedAt >= monthStart && x.ChangedAt < monthEnd && (x.ToStatus == LeadStatus.Lost || x.ToStatus == LeadStatus.NotInterested)).Select(x => x.LeadId).Distinct().Count();
            var monthReturned = returns.Count(x => x.EmployeeId == employee.Id && x.ReturnedAt >= monthStart && x.ReturnedAt < monthEnd);
            var monthAssigned = assignmentRows.Where(x => x.EmployeeId == employee.Id && x.ChangedAt >= monthStart && x.ChangedAt < monthEnd).Select(x => x.LeadId).Distinct().Count();
            var monthFollowed = followRows.Where(x => x.EmployeeId == employee.Id && x.CreatedAt >= monthStart && x.CreatedAt < monthEnd).Select(x => x.LeadId).Distinct().Count();
            var units = bookings.Count(x => x.EmployeeId == employee.Id && x.BookedAt >= monthStart && x.BookedAt < monthEnd); var unitTarget = targets.Where(x => x.EmployeeId == employee.Id && x.Month == month).Sum(x => x.MinimumSalesUnits);
            var collected = collections.Where(x => x.EmployeeId == employee.Id && x.Month == month).Sum(x => x.Amount); var collectionTarget = targets.Where(x => x.EmployeeId == employee.Id && x.Month == month).Sum(x => x.MinimumCollectionAmount);
            var response = monthAssigned == 0 ? 0 : Cap(100 - Rate(monthReturned, monthAssigned)); var sales = unitTarget == 0 ? (units > 0 ? 100 : 0) : Cap(Rate(units, unitTarget)); var collection = collectionTarget == 0 ? (collected > 0 ? 100 : 0) : Cap(Rate(collected, collectionTarget)); var win = Rate(monthWon, monthWon + monthLost); var followup = monthAssigned == 0 ? 0 : Cap(Rate(monthFollowed, monthAssigned)); var score = Math.Round(response * .30m + sales * .25m + collection * .20m + win * .15m + followup * .10m, 2);
            return new { employeeId = employee.Id, employee = employee.FullName, month, score, rating = score >= 85 ? "Excellent" : score >= 70 ? "Good" : score >= 50 ? "Needs improvement" : "Critical", won = monthWon, lost = monthLost, returned = monthReturned, salesUnits = units, salesTarget = unitTarget, collections = collected, collectionTarget, responseDiscipline = response, winRate = win, followupCoverage = followup };
        })).OrderByDescending(x => x.month).ToList();

        return Ok(new { from = start, to = finish, generatedAt = DateTime.UtcNow, kpis, employeeMonthly, leadSummary, salesSummary, collectionSummary, returnedLeads = returns, projectSummary, teamSummary, groupSummary, formula = new { score = "Response discipline x 30% + sales achievement x 25% + collection achievement x 20% + win rate x 15% + follow-up coverage x 10%", responseDiscipline = "100 - (returned leads / assigned leads x 100)", salesAchievement = "sales units / sales-unit target x 100", collectionAchievement = "collections / collection target x 100", winRate = "won / (won + lost) x 100", followupCoverage = "distinct assigned leads followed up / assigned leads x 100", note = "Component scores are capped at 100%. Returned leads have the highest individual weight." } });
        /* Obsolete response retained only as a source-history marker.

        return Ok(new { from = start, to = finish, generatedAt = DateTime.UtcNow, kpis, leadSummary, salesSummary, collectionSummary, returnedLeads = returns, projectSummary, formula = new { score = "Response discipline × 30% + sales achievement × 25% + collection achievement × 20% + win rate × 15% + follow-up coverage × 10%", responseDiscipline = "100 − (returned leads ÷ assigned leads × 100)", salesAchievement = "sales units ÷ sales-unit target × 100", collectionAchievement = "collections ÷ collection target × 100", winRate = "won ÷ (won + lost) × 100", followupCoverage = "distinct assigned leads followed up ÷ assigned leads × 100", note = "Component scores are capped at 100%. Returned leads have the highest individual weight." } });
        */
    }
}
