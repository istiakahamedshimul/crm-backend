using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Services;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Authorize]
[backend.Security.RequirePermission(PermissionCodes.ReportsView)]
[Route("api/reports")]
[Tags("Reports")]
public class ReportsController(CrmDbContext db, IFinancialService financial, IReportingService reporting) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<ActionResult> Kpis([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int? salesExecutiveId = null, [FromQuery] int? projectId = null, [FromQuery] int? teamId = null, [FromQuery] int? groupId = null, [FromQuery] LeadSource? source = null, [FromQuery] LeadStatus? leadStatus = null, [FromQuery] LeadPriority? priority = null)
    {
        var today = DateTime.UtcNow.Date; var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date; var end = (to ?? today).Date;
        if (start > end) return BadRequest(new { message = "Start date cannot be after end date." });
        if ((end - start).TotalDays > 3660) return BadRequest(new { message = "Report period cannot exceed ten years." });
        if (User.IsInRole("SalesExecutive")) salesExecutiveId = backend.Extensions.ClaimsPrincipalExtensions.UserId(User);
        if (User.IsInRole("GroupLeader")) { var ownGroup = await db.SalesGroups.Where(x => x.GroupLeaderId == backend.Extensions.ClaimsPrincipalExtensions.UserId(User)).Select(x => (int?)x.Id).SingleOrDefaultAsync(); if (!ownGroup.HasValue) return Forbid(); groupId = ownGroup; salesExecutiveId = null; teamId = null; }
        var filter = new ReportingFilter(start, end, salesExecutiveId, projectId, teamId, groupId, source, leadStatus, priority);
        var result = await reporting.KpisAsync(filter); await Audit("kpis", "View", filter); return Ok(new { from = start, to = end, generatedAt = DateTime.UtcNow, kpis = result });
    }

    [HttpGet("catalog")]
    public ActionResult Catalog() => Ok(ReportCatalog.All);

    [HttpGet("drilldown/{key}")]
    public async Task<ActionResult> Drilldown(string key, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? salesExecutiveId = null, [FromQuery] int? projectId = null)
    {
        var end = to.Date.AddDays(1); if (User.IsInRole("SalesExecutive")) salesExecutiveId = backend.Extensions.ClaimsPrincipalExtensions.UserId(User);
        object rows = key switch
        {
            "lead-assignment-rate" or "lead-contact-conversion" or "contact-interest-conversion" or "lead-booking-conversion" or "win-rate" or "loss-rate" or "repeat-referral" or "project-conversion" => await db.Leads.AsNoTracking().Where(x => x.CreatedAt >= from.Date && x.CreatedAt < end && (!salesExecutiveId.HasValue || x.AssignedToId == salesExecutiveId) && (!projectId.HasValue || x.ProjectId == projectId)).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, name = x.CustomerName, x.Phone, x.Status, x.Priority, x.Source, project = x.Project == null ? null : x.Project.Name, employee = x.AssignedTo == null ? null : x.AssignedTo.FullName, date = x.CreatedAt }).Take(2000).ToListAsync(),
            "first-contact-sla" or "follow-up-completion" or "overdue-follow-up" or "follow-up-proof" or "employee-activity" => await db.FollowUps.AsNoTracking().Where(x => x.CreatedAt >= from.Date && x.CreatedAt < end && (!salesExecutiveId.HasValue || x.CreatedById == salesExecutiveId)).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.LeadId, name = x.Lead.CustomerName, employee = x.CreatedBy.FullName, x.Type, x.ResultingStatus, x.NextFollowUpAt, proofCount = x.Proofs.Count, date = x.CreatedAt }).Take(2000).ToListAsync(),
            "site-visit-utilization" or "vehicle-utilization" or "interest-visit-conversion" or "visit-booking-conversion" => await db.VehicleBookings.AsNoTracking().Where(x => x.CreatedAt >= from.Date && x.CreatedAt < end && (!salesExecutiveId.HasValue || x.SalesExecutiveId == salesExecutiveId) && (!projectId.HasValue || x.ProjectId == projectId)).OrderByDescending(x => x.VisitDate).Select(x => new { x.Id, name = x.Customer == null ? null : x.Customer.Name, employee = x.SalesExecutive.FullName, project = x.Project == null ? null : x.Project.Name, x.VisitDate, x.VisitTime, x.Status, vehicle = x.Vehicle == null ? null : x.Vehicle.RegistrationNumber, date = x.CreatedAt }).Take(2000).ToListAsync(),
            "collection-target" or "collection-efficiency" or "average-collection-booking" or "commission-collection" or "period-growth" => await db.MonthlyCollections.AsNoTracking().Where(x => x.Month >= DateOnly.FromDateTime(from) && x.Month <= DateOnly.FromDateTime(to) && (!salesExecutiveId.HasValue || x.SalesExecutiveId == salesExecutiveId)).OrderByDescending(x => x.Month).Select(x => new { x.Id, name = x.SalesExecutive.FullName, team = x.SalesExecutive.SalesTeam == null ? null : x.SalesExecutive.SalesTeam.Name, x.Month, x.Amount, x.Remarks, date = x.UpdatedAt }).Take(2000).ToListAsync(),
            "outstanding-ratio" or "overdue-ratio" or "emi-default" => await db.EmiInstallments.AsNoTracking().Where(x => !salesExecutiveId.HasValue || x.FinancialAgreement.Customer.AssignedToId == salesExecutiveId).OrderBy(x => x.DueDate).Select(x => new { x.Id, name = x.FinancialAgreement.Customer.Name, fileId = x.FinancialAgreement.Customer.FileId, x.InstallmentNumber, x.DueDate, x.ExpectedAmount, x.PaidAmount, outstanding = x.ExpectedAmount - x.PaidAmount, x.Status, date = x.DueDate }).Take(2000).ToListAsync(),
            "average-invoice-value" => await db.Invoices.AsNoTracking().Where(x => x.CreatedAt >= from.Date && x.CreatedAt < end && (!salesExecutiveId.HasValue || x.SalesExecutiveId == salesExecutiveId)).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, name = x.Customer.Name, x.InvoiceNumber, x.FinalAmount, x.Status, employee = x.SalesExecutive.FullName, date = x.CreatedAt }).Take(2000).ToListAsync(),
            "daily-work-report" => await db.DailyWorkReports.AsNoTracking().Where(x => x.WorkDate >= DateOnly.FromDateTime(from) && x.WorkDate <= DateOnly.FromDateTime(to) && (!salesExecutiveId.HasValue || x.SalesExecutiveId == salesExecutiveId)).OrderByDescending(x => x.WorkDate).Select(x => new { x.Id, name = x.SalesExecutive.FullName, x.WorkDate, x.Summary, date = x.CreatedAt }).Take(2000).ToListAsync(),
            _ => await db.Leads.AsNoTracking().Where(x => x.CreatedAt >= from.Date && x.CreatedAt < end).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, name = x.CustomerName, x.Phone, x.Status, date = x.CreatedAt }).Take(2000).ToListAsync()
        };
        await Audit(key, "Drilldown", new { from, to, salesExecutiveId, projectId }); return Ok(new { key, from, to, rows });
    }

    [HttpGet("export/{key}.csv")]
    public async Task<ActionResult> ExportCsv(string key, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var end = to.Date.AddDays(1); var rows = await db.Leads.AsNoTracking().Where(x => x.CreatedAt >= from.Date && x.CreatedAt < end).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.CustomerName, x.Phone, x.Email, Status = x.Status.ToString(), Priority = x.Priority.ToString(), Source = x.Source.ToString(), Project = x.Project == null ? "" : x.Project.Name, Employee = x.AssignedTo == null ? "" : x.AssignedTo.FullName, x.CreatedAt }).ToListAsync();
        static string Q(object? value) => $"\"{Convert.ToString(value)?.Replace("\"", "\"\"")}\"";
        var csv = new System.Text.StringBuilder("Id,Name,Phone,Email,Status,Priority,Source,Project,Employee,CreatedAt\r\n"); foreach (var x in rows) csv.AppendLine(string.Join(',', Q(x.Id), Q(x.CustomerName), Q(x.Phone), Q(x.Email), Q(x.Status), Q(x.Priority), Q(x.Source), Q(x.Project), Q(x.Employee), Q(x.CreatedAt)));
        await Audit(key, "ExportCsv", new { from, to }); return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", $"{key}-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private async Task Audit(string key, string action, object filters) { db.ReportAccessAudits.Add(new ReportAccessAudit { UserId = backend.Extensions.ClaimsPrincipalExtensions.UserId(User), ReportKey = key, Action = action, FiltersJson = System.Text.Json.JsonSerializer.Serialize(filters) }); await db.SaveChangesAsync(); }
    [HttpGet("basic")]
    public async Task<ActionResult> GetBasicReport()
    {
        if (User.IsInRole("GroupLeader")) return Forbid();
        var collectionRows = await db.MonthlyCollections.Select(x => new { x.Amount }).ToListAsync();
        return Ok(new
        {
            leadStatus = await db.Leads.GroupBy(x => x.Status).Select(x => new { status = x.Key, count = x.Count() }).ToListAsync(),
            leadSource = await db.Leads.GroupBy(x => x.Source).Select(x => new { source = x.Key, count = x.Count() }).ToListAsync(),
            paymentStatus = new[] { new { status = PaymentStatus.Approved, amount = collectionRows.Sum(x => x.Amount), count = collectionRows.Count } },
            invoiceStatus = await db.Invoices.GroupBy(x => x.Status).Select(x => new { status = x.Key, amount = x.Sum(i => i.FinalAmount), count = x.Count() }).ToListAsync()
        });
    }

    [HttpGet("financial")]
    public async Task<ActionResult> Financial([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,[FromQuery]int? salesExecutiveId=null)
    {
        if (User.IsInRole("GroupLeader")) return Forbid();
        var customers=db.Customers.AsQueryable();if(salesExecutiveId.HasValue)customers=customers.Where(x=>x.AssignedToId==salesExecutiveId);var ids=await customers.Select(x=>x.Id).ToListAsync();var summaries=new List<FinancialSummary>();foreach(var id in ids)summaries.Add(await financial.SummaryAsync(id));
        var collections=db.MonthlyCollections.AsQueryable();if(from.HasValue)collections=collections.Where(x=>x.Month>=new DateOnly(from.Value.Year,from.Value.Month,1));if(to.HasValue)collections=collections.Where(x=>x.Month<=new DateOnly(to.Value.Year,to.Value.Month,1));if(salesExecutiveId.HasValue)collections=collections.Where(x=>x.SalesExecutiveId==salesExecutiveId);
        return Ok(new{totalCollectible=summaries.Sum(x=>x.TotalAgreedAmount),totalCollected=await collections.SumAsync(x=>(decimal?)x.Amount)??0,totalOutstanding=summaries.Sum(x=>x.OutstandingBalance),totalDue=summaries.Sum(x=>x.CurrentDue),totalOverdue=summaries.Sum(x=>x.OverdueAmount),customersWithOverdue=summaries.Count(x=>x.OverdueAmount>0),collectionBySalesExecutive=await collections.GroupBy(x=>x.SalesExecutiveId).Select(x=>new{salesExecutiveId=x.Key,amount=x.Sum(p=>p.Amount),count=x.Count()}).ToListAsync()});
    }
}

file static class ReportCatalog
{
    public static readonly object[] All =
    [
        new { key="executive", name="Executive dashboard", reports=new[]{"Lead overview","Sales and booking overview","CA employee-month collections","Dues and overdue","Targets","Commissions","Visits","Projects","Critical alerts"}},
        new { key="leads", name="Lead reports", reports=new[]{"Complete register","Created by date","Status","Priority","Source","Project","Preferred location","Budget","Creator","Assigned employee","Assigned vs unassigned","Assignment delay/history","Returned/automation returned","No first follow-up","Inactive/overdue/stale","Duplicates","Repeat/referral","Lost/not interested","Aging","Source quality/conversion","Workload","Reassignment"}},
        new { key="pipeline", name="Pipeline and funnel", reports=new[]{"Complete funnel","Stage conversions","Stage drop-off","Current pipeline","Pipeline by project/employee/team/source","Stage age","Velocity","Stalled opportunities","Period/employee/project comparison","Win/loss","Sales cycle","Cohorts"}},
        new { key="followups", name="Follow-up and activity", reports=new[]{"History","By employee/lead/customer/type/result","Period counts","Completed vs scheduled","Missed/overdue","Response time","First contact","Before booking/loss","Effectiveness","Proof compliance/distribution","Activity calendar","Next actions","Consistency","Customer timeline"}},
        new { key="visits", name="Site visit and transportation", reports=new[]{"Request register","Pending/approved/rejected/cancelled/upcoming","By project/customer/employee/team/vehicle/driver","Vehicle utilization/conflicts","Passengers","Pickup locations","Approval time","Cancellation reasons","Visit conversion","Employee success","Project popularity","Repeat visits","Transport calendar"}},
        new { key="collections", name="CA monthly collections", reports=new[]{"Employee monthly register","By month/employee/team/group","Company total","Selected period","Trends","Target achievement/variance","Below/above target","Leaderboard","Period growth","Entry/correction audit","Missing monthly entries","Duplicate/conflict exceptions"}},
        new { key="dues", name="Customer dues", reports=new[]{"Register","Unpaid/paid/cancelled","Created/paid today","By month/customer/employee/project/team/group","Current/overdue","Aging","Multiple/high-value dues","Notification history/read status","CA paid actions","Days to paid","Status history/reopened/exceptions"}},
        new { key="finance", name="Finance, EMI, invoice and commission", reports=new[]{"Agreed value","Outstanding","EMI schedules/defaults","Invoice register/status/aging/value","Commission register/status/liability/ratio","Financial audit"}},
        new { key="employees", name="Employees and hierarchy", reports=new[]{"Employee scorecard","Group/team/sub-team performance","Targets","Lead workload","Activity/proof/work-report compliance","Location compliance","Leaderboards","Trends"}},
        new { key="quality", name="Quality, security and audit", reports=new[]{"Missing/invalid data","Orphaned records","Financial exceptions","Roles and permissions","Record audit","Report access","Export/print audit","Sensitive-data access"}}
    ];
}
