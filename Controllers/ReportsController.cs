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
        return Ok(new
        {
            leadStatus = await db.Leads.GroupBy(x => x.Status).Select(x => new { status = x.Key, count = x.Count() }).ToListAsync(),
            leadSource = await db.Leads.GroupBy(x => x.Source).Select(x => new { source = x.Key, count = x.Count() }).ToListAsync(),
            paymentStatus = await db.Payments.GroupBy(x => x.Status).Select(x => new { status = x.Key, amount = x.Sum(p => p.Amount), count = x.Count() }).ToListAsync(),
            invoiceStatus = await db.Invoices.GroupBy(x => x.Status).Select(x => new { status = x.Key, amount = x.Sum(i => i.FinalAmount), count = x.Count() }).ToListAsync()
        });
    }

    [HttpGet("financial")]
    public async Task<ActionResult> Financial([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,[FromQuery]int? salesExecutiveId=null)
    {
        var customers=db.Customers.AsQueryable();if(salesExecutiveId.HasValue)customers=customers.Where(x=>x.AssignedToId==salesExecutiveId);var ids=await customers.Select(x=>x.Id).ToListAsync();var summaries=new List<FinancialSummary>();foreach(var id in ids)summaries.Add(await financial.SummaryAsync(id));
        var payments=db.Payments.Where(x=>x.Status==PaymentStatus.Approved&&!x.IsReversed);if(from.HasValue)payments=payments.Where(x=>x.PaymentDate>=from.Value);if(to.HasValue)payments=payments.Where(x=>x.PaymentDate<to.Value.AddDays(1));if(salesExecutiveId.HasValue)payments=payments.Where(x=>x.SalesExecutiveId==salesExecutiveId);
        return Ok(new{totalCollectible=summaries.Sum(x=>x.TotalAgreedAmount),totalCollected=await payments.SumAsync(x=>(decimal?)x.Amount)??0,totalOutstanding=summaries.Sum(x=>x.OutstandingBalance),totalDue=summaries.Sum(x=>x.CurrentDue),totalOverdue=summaries.Sum(x=>x.OverdueAmount),customersWithOverdue=summaries.Count(x=>x.OverdueAmount>0),collectionBySalesExecutive=await payments.GroupBy(x=>x.SalesExecutiveId).Select(x=>new{salesExecutiveId=x.Key,amount=x.Sum(p=>p.Amount),count=x.Count()}).ToListAsync()});
    }
}
