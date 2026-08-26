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
public class ReportsController(CrmDbContext db, IFinancialService financial) : ControllerBase
{
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
