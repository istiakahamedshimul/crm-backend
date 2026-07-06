using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin,Manager,Accountant")]
[Route("api/reports")]
[Tags("Reports")]
public class ReportsController(CrmDbContext db) : ControllerBase
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
}
