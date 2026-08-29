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
public class ReportsController(CrmDbContext db, IFinancialService financial, IReportingService reporting, IOperationalReportService operational) : ControllerBase
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
    public ActionResult Catalog() => Ok(ProfessionalReportCatalog.All);

    [HttpGet("overview")]
    public async Task<ActionResult> Overview([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,[FromQuery]int? salesExecutiveId=null,[FromQuery]int? projectId=null)
    {
        var today=DateTime.UtcNow.Date;var start=(from??new DateTime(today.Year,today.Month,1)).Date;var finish=(to??today).Date;var end=finish.AddDays(1);if(start>finish)return BadRequest(new{message="Start date cannot be after end date."});if(User.IsInRole("SalesExecutive"))salesExecutiveId=backend.Extensions.ClaimsPrincipalExtensions.UserId(User);
        var leads=db.Leads.AsNoTracking().AsQueryable();var customers=db.Customers.AsNoTracking().AsQueryable();if(salesExecutiveId.HasValue){leads=leads.Where(x=>x.AssignedToId==salesExecutiveId);customers=customers.Where(x=>x.AssignedToId==salesExecutiveId);}if(projectId.HasValue){leads=leads.Where(x=>x.ProjectId==projectId);customers=customers.Where(x=>x.ProjectId==projectId);}
        var periodLeads=leads.Where(x=>x.CreatedAt>=start&&x.CreatedAt<end);var booked=customers.Where(x=>x.BookedAt>=start&&x.BookedAt<end);var monthFrom=new DateOnly(start.Year,start.Month,1);var monthTo=new DateOnly(finish.Year,finish.Month,1);var collections=db.MonthlyCollections.AsNoTracking().Where(x=>x.Month>=monthFrom&&x.Month<=monthTo);if(salesExecutiveId.HasValue)collections=collections.Where(x=>x.SalesExecutiveId==salesExecutiveId);
        var currentMonth=new DateOnly(today.Year,today.Month,1);var previousMonth=currentMonth.AddMonths(-1);var lastYear=currentMonth.AddYears(-1);var currentCollection=await db.MonthlyCollections.Where(x=>x.Month==currentMonth).SumAsync(x=>(decimal?)x.Amount)??0;var previousCollection=await db.MonthlyCollections.Where(x=>x.Month==previousMonth).SumAsync(x=>(decimal?)x.Amount)??0;var lastYearCollection=await db.MonthlyCollections.Where(x=>x.Month==lastYear).SumAsync(x=>(decimal?)x.Amount)??0;
        var totalAgreed=await db.FinancialAgreements.SumAsync(x=>(decimal?)x.TotalAgreedAmount)??0;var outstanding=await db.EmiInstallments.SumAsync(x=>(decimal?)(x.ExpectedAmount-x.PaidAmount))??0;var unpaidDues=db.CustomerDues.Where(x=>x.Status==CustomerDueStatus.Unpaid);var currentDue=await unpaidDues.Where(x=>x.DueDate>=today).SumAsync(x=>(decimal?)x.Amount)??0;var overdue=await unpaidDues.Where(x=>x.DueDate<today).SumAsync(x=>(decimal?)x.Amount)??0;
        var unitTarget=await db.MonthlySalesTargets.Where(x=>x.Month>=monthFrom&&x.Month<=monthTo).SumAsync(x=>(int?)x.MinimumSalesUnits)??0;var collectionTarget=await db.MonthlySalesTargets.Where(x=>x.Month>=monthFrom&&x.Month<=monthTo).SumAsync(x=>(decimal?)x.MinimumCollectionAmount)??0;var bookedCount=await booked.CountAsync();var periodCollection=await collections.SumAsync(x=>(decimal?)x.Amount)??0;
        var topProject=await booked.Where(x=>x.ProjectId!=null).GroupBy(x=>new{x.ProjectId,x.Project!.Name}).Select(g=>new{g.Key.Name,value=g.Count()}).OrderByDescending(x=>x.value).FirstOrDefaultAsync();var topEmployee=await booked.Where(x=>x.BookedById!=null).GroupBy(x=>new{x.BookedById,x.BookedBy!.FullName}).Select(g=>new{g.Key.FullName,value=g.Count()}).OrderByDescending(x=>x.value).FirstOrDefaultAsync();var topGroup=await booked.Where(x=>x.BookedBy!=null&&x.BookedBy.SalesTeam!=null).GroupBy(x=>new{x.BookedBy!.SalesTeam!.SalesGroupId,x.BookedBy.SalesTeam.SalesGroup.Name}).Select(g=>new{g.Key.Name,value=g.Count()}).OrderByDescending(x=>x.value).FirstOrDefaultAsync();
        var totalLeads=await leads.CountAsync();var assigned=await leads.CountAsync(x=>x.AssignedToId!=null);var lost=await periodLeads.CountAsync(x=>x.Status==LeadStatus.Lost||x.Status==LeadStatus.NotInterested);var activeStatuses=new[]{LeadStatus.New,LeadStatus.Assigned,LeadStatus.Contacted,LeadStatus.Interested,LeadStatus.FollowUpNeeded,LeadStatus.SiteVisitScheduled,LeadStatus.Visited,LeadStatus.Negotiation};var active=await leads.CountAsync(x=>activeStatuses.Contains(x.Status));
        return Ok(new{generatedAt=DateTime.UtcNow,metrics=new object[]{M("total-leads","Total leads",totalLeads,"count"),M("new-leads-today","New leads today",await leads.CountAsync(x=>x.CreatedAt>=today),"count"),M("leads-assigned","Leads assigned",assigned,"count"),M("unassigned-leads","Unassigned leads",totalLeads-assigned,"count"),M("active-opportunities","Active opportunities",active,"count"),M("customers-booked","Customers booked",bookedCount,"count"),M("lost-leads","Lost and not interested",lost,"count"),M("lead-booking-conversion","Lead-to-booking conversion",KpiFormulas.Rate(bookedCount,await periodLeads.CountAsync()),"percent"),M("agreed-sales-value","Total agreed sales value",totalAgreed,"money"),M("total-collected","Total amount collected",await db.MonthlyCollections.SumAsync(x=>(decimal?)x.Amount)??0,"money"),M("period-collection","Collection during selected period",periodCollection,"money"),M("outstanding","Total outstanding balance",outstanding,"money"),M("current-due","Current due",currentDue,"money"),M("overdue","Overdue amount",overdue,"money"),M("overdue-customers","Customers with overdue installments",await unpaidDues.Where(x=>x.DueDate<today).Select(x=>x.CustomerId).Distinct().CountAsync(),"count"),M("collection-target","Collection target achievement",KpiFormulas.Rate(periodCollection,collectionTarget),"percent"),M("sales-target","Sales-unit target achievement",KpiFormulas.Rate(bookedCount,unitTarget),"percent"),M("pending-commission","Pending commissions",await db.Commissions.Where(x=>x.Status==CommissionStatus.Pending).SumAsync(x=>(decimal?)x.Amount)??0,"money"),M("paid-commission","Paid commissions",await db.Commissions.Where(x=>x.Status==CommissionStatus.Paid).SumAsync(x=>(decimal?)x.Amount)??0,"money"),M("upcoming-visits","Upcoming site visits",await db.VehicleBookings.CountAsync(x=>x.VisitDate>=DateOnly.FromDateTime(today)&&x.Status==VehicleBookingStatus.Approved),"count"),M("cancelled-visits","Cancelled visits",await db.VehicleBookings.CountAsync(x=>x.Status==VehicleBookingStatus.Cancelled&&x.CreatedAt>=start&&x.CreatedAt<end),"count"),M("active-projects","Active projects",await db.Projects.CountAsync(x=>x.Status==ProjectStatus.Upcoming||x.Status==ProjectStatus.Ongoing||x.Status==ProjectStatus.Ready),"count"),new{key="top-project",name="Top-performing project",value=topProject?.value??0,unit="count",label=topProject?.Name??"No data"},new{key="top-employee",name="Top sales executive",value=topEmployee?.value??0,unit="count",label=topEmployee?.FullName??"No data"},new{key="top-group",name="Top sales group/team",value=topGroup?.value??0,unit="count",label=topGroup?.Name??"No data"},M("mom-growth","Month-over-month growth",KpiFormulas.Growth(currentCollection,previousCollection),"percent"),M("yoy-growth","Year-over-year growth",KpiFormulas.Growth(currentCollection,lastYearCollection),"percent"),M("critical-alerts","Critical alerts and exceptions",(totalLeads-assigned)+await unpaidDues.Where(x=>x.DueDate<today).CountAsync()+await db.Leads.CountAsync(x=>x.NextFollowUpAt<today),"count")},collectionSource="CA-entered employee/month totals only"});
    }

    private static object M(string key,string name,decimal value,string unit)=>new{key,name,value,unit,label=(string?)null};

    [HttpGet("data/{key}")]
    public async Task<ActionResult> ReportData(string key,[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,[FromQuery]int? salesExecutiveId=null,[FromQuery]int? projectId=null,[FromQuery]int? teamId=null,[FromQuery]int? groupId=null,[FromQuery]LeadSource? source=null,[FromQuery]LeadStatus? leadStatus=null,[FromQuery]LeadPriority? priority=null)
    {
        var today=DateTime.UtcNow.Date;var start=(from??new DateTime(today.Year,today.Month,1)).Date;var finish=(to??today).Date;if(start>finish)return BadRequest(new{message="Start date cannot be after end date."});
        if(User.IsInRole("SalesExecutive"))salesExecutiveId=backend.Extensions.ClaimsPrincipalExtensions.UserId(User);
        if(User.IsInRole("GroupLeader")){var own=await db.SalesGroups.Where(x=>x.GroupLeaderId==backend.Extensions.ClaimsPrincipalExtensions.UserId(User)).Select(x=>(int?)x.Id).SingleOrDefaultAsync();if(!own.HasValue)return Forbid();groupId=own;salesExecutiveId=null;teamId=null;}
        var filter=new ReportingFilter(start,finish,salesExecutiveId,projectId,teamId,groupId,source,leadStatus,priority);var result=await operational.BuildAsync(key,filter);if(result is null)return NotFound(new{message="Unknown report."});await Audit(key,"ViewReport",filter);return Ok(new{from=start,to=finish,generatedAt=DateTime.UtcNow,report=result});
    }

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
            collectionSource = "CA employee-month totals only"
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
        new { key="finance", name="Finance, EMI and commission", reports=new[]{"Agreed value","Outstanding","EMI schedules/defaults","CA employee-month collection","Commission register/status/liability/ratio","Financial audit"}},
        new { key="employees", name="Employees and hierarchy", reports=new[]{"Employee scorecard","Group/team/sub-team performance","Targets","Lead workload","Activity/proof/work-report compliance","Location compliance","Leaderboards","Trends"}},
        new { key="quality", name="Quality, security and audit", reports=new[]{"Missing/invalid data","Orphaned records","Financial exceptions","Roles and permissions","Record audit","Report access","Export/print audit","Sensitive-data access"}}
    ];
}

file static class ProfessionalReportCatalog
{
    private static object I(string key,string name,bool available=true,string? note=null)=>new{key,name,available,note};
    public static readonly object[] All=
    [
      new{key="leads",name="Lead reports",items=new[]{I("lead-register","Complete lead register"),I("lead-created-date","Leads created by date"),I("lead-by-status","Leads by status"),I("lead-by-priority","Leads by priority"),I("lead-by-source","Leads by source"),I("lead-by-project","Leads by project"),I("lead-by-location","Leads by preferred location"),I("lead-by-budget","Leads by budget range"),I("lead-by-creator","Leads by creator"),I("lead-by-employee","Leads by assigned employee"),I("lead-assigned","Assigned leads"),I("lead-unassigned","Unassigned leads"),I("lead-newly-assigned","Newly assigned leads"),I("lead-no-first-followup","Leads without first follow-up"),I("lead-inactive","Leads without recent activity"),I("lead-overdue-followup","Overdue follow-up leads"),I("lead-followup-today","Follow-ups due today"),I("lead-future-followup","Future follow-up schedule"),I("lead-stale","Stale or aging leads"),I("lead-duplicates","Duplicate phone analysis"),I("lead-repeat","Repeat-customer leads"),I("lead-referral","Referral leads"),I("lead-referrer-contribution","Referrer contribution"),I("lead-lost","Lost leads"),I("lead-not-interested","Not-interested leads"),I("lead-aging","Lead aging"),I("lead-source-quality","Lead source quality"),I("lead-source-conversion","Lead source-to-booking conversion"),I("lead-project-interest","Lead project interest"),I("lead-workload","Employee lead workload"),I("lead-lost-reasons","Lost-reason analysis",false,"Standardized lost reasons are not stored yet")}},
      new{key="pipeline",name="Sales pipeline and funnel",items=new[]{I("pipeline-funnel","Complete sales funnel"),I("pipeline-stage-dropoff","Stage-to-stage drop-off"),I("pipeline-register","Current pipeline register"),I("pipeline-by-project","Pipeline by project"),I("pipeline-by-employee","Pipeline by employee"),I("pipeline-by-source","Pipeline by source"),I("pipeline-conversions","Stage conversion rates"),I("pipeline-win-loss","Win/loss analysis"),I("pipeline-sales-cycle","Sales cycle duration"),I("pipeline-period-comparison","Period comparison"),I("pipeline-expected-value","Pipeline by expected value",false,"Expected values are not stored")}},
      new{key="followups",name="Follow-up and activity",items=new[]{I("followup-register","Complete follow-up history"),I("followup-by-employee","Follow-ups by employee"),I("followup-by-type","Follow-ups by communication type"),I("followup-by-result","Follow-ups by resulting status"),I("followup-counts","Daily follow-up counts"),I("followup-missed","Missed follow-ups"),I("followup-overdue","Overdue follow-ups"),I("followup-with-proof","Follow-ups with proof"),I("followup-without-proof","Follow-ups without proof"),I("followup-proof-distribution","Proof compliance"),I("followup-activity","Call/meeting/site-visit activity"),I("followup-next-actions","Next-action report"),I("followup-effectiveness-type","Effectiveness by type"),I("followup-effectiveness-employee","Effectiveness by employee")}},
      new{key="visits",name="Site visit and transportation",items=new[]{I("visit-register","Site-visit request register"),I("visit-pending","Pending approvals"),I("visit-approved","Approved visits"),I("visit-rejected","Rejected visits"),I("visit-cancelled","Cancelled visits"),I("visit-upcoming","Upcoming visits"),I("visit-by-project","Visits by project"),I("visit-by-customer","Visits by customer"),I("visit-by-employee","Visits by sales employee"),I("visit-by-vehicle","Visits by vehicle"),I("visit-by-driver","Visits by driver"),I("visit-vehicle-utilization","Vehicle utilization"),I("visit-passengers","Passenger count"),I("visit-pickup","Pickup locations"),I("visit-cancellation-reasons","Cancellation reasons"),I("visit-project-popularity","Project visit popularity"),I("visit-repeat","Repeat visits"),I("visit-completed","Completed visits",false,"Completed status is not stored"),I("visit-no-show","No-show visits",false,"No-show status is not stored"),I("visit-operating-cost","Vehicle operating cost",false,"Fuel and expense data are not stored")}},
      new{key="collections",name="CA employee collection",items=new[]{I("collection-register","Employee-month collection register"),I("collection-by-month","Collection by month"),I("collection-by-employee","Collection by employee"),I("collection-by-team","Collection by team"),I("collection-by-group","Collection by group"),I("collection-trend","Collection trend")}}
    ];
}
