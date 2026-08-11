using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/dashboard"), Tags("Dashboard")]
public class DashboardController(CrmDbContext db, IFinancialService financial) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var customerIds = User.IsInRole("SalesExecutive")
            ? await db.Customers.Where(x => x.AssignedToId == User.UserId()).Select(x => x.Id).ToListAsync()
            : await db.Customers.Select(x => x.Id).ToListAsync();
        var summaries = new List<FinancialSummary>();
        foreach (var id in customerIds) summaries.Add(await financial.SummaryAsync(id));
        if (User.IsInRole("SalesExecutive"))
            return Ok(new { assignedCustomers = customerIds.Count, customersWithCurrentDues = summaries.Count(x => x.CurrentDue > 0), customersWithOverduePayments = summaries.Count(x => x.OverdueAmount > 0), totalOutstanding = summaries.Sum(x => x.OutstandingBalance), upcomingEmiReminders = summaries.Count(x => x.NextEmiDueDate.HasValue) });

        var today = DateTime.UtcNow.Date;
        var rangeFrom = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var rangeTo = (to ?? today).Date;
        if (rangeFrom > rangeTo) return BadRequest(new { message = "Collection start date cannot be after the end date." });
        var endExclusive = rangeTo.AddDays(1);
        var approvedInRange = db.Payments.Where(x => x.Status == PaymentStatus.Approved && !x.IsReversed && x.PaymentDate >= rangeFrom && x.PaymentDate < endExclusive);

        return Ok(new
        {
            leads = await db.Leads.CountAsync(),
            customers = customerIds.Count,
            projects = await db.Projects.CountAsync(x => x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.SoldOut && x.Status != ProjectStatus.Paused),
            totalCollectible = summaries.Sum(x => x.TotalAgreedAmount),
            totalCollected = summaries.Sum(x => x.TotalPaid),
            totalCollection = await approvedInRange.SumAsync(x => (decimal?)x.Amount) ?? 0,
            collectionCount = await approvedInRange.CountAsync(),
            collectionFrom = rangeFrom,
            collectionTo = rangeTo,
            totalOutstanding = summaries.Sum(x => x.OutstandingBalance),
            totalDue = summaries.Sum(x => x.CurrentDue),
            totalOverdue = summaries.Sum(x => x.OverdueAmount),
            customersWithOverdueInstallments = summaries.Count(x => x.OverdueAmount > 0),
            pendingPayments = await db.Payments.CountAsync(x => x.Status == PaymentStatus.Pending),
            approvedPayments = await db.Payments.CountAsync(x => x.Status == PaymentStatus.Approved && !x.IsReversed)
        });
    }
}
