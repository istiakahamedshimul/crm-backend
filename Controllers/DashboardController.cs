using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
[Tags("Dashboard")]
public class DashboardController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetSummary()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var totalCollection = await db.Payments
            .Where(x => x.Status == PaymentStatus.Approved &&
                        x.VerifiedAt >= monthStart &&
                        x.VerifiedAt < nextMonthStart)
            .SumAsync(x => x.Amount);
        return Ok(new
        {
            leads = await db.Leads.CountAsync(),
            customers = await db.Leads.CountAsync(x => x.Status == LeadStatus.Booked),
            projects = await db.Projects.CountAsync(),
            invoices = await db.Invoices.CountAsync(),
            unpaidInvoices = await db.Invoices.CountAsync(x => x.Status != InvoiceStatus.Paid),
            pendingPayments = await db.Payments.CountAsync(x => x.Status == PaymentStatus.Pending),
            approvedPayments = await db.Payments.CountAsync(x => x.Status == PaymentStatus.Approved),
            totalCollection,
            pendingCommission = await db.Commissions.Where(x => x.Status == CommissionStatus.Pending).SumAsync(x => x.Amount),
            paidCommission = await db.Commissions.Where(x => x.Status == CommissionStatus.Paid).SumAsync(x => x.Amount)
        });
    }
}
