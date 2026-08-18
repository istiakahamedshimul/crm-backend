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
    [HttpGet("sales-report")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    public async Task<ActionResult> GetSalesReport([FromQuery] int salesExecutiveId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var rangeFrom = from.Date;
        var rangeTo = to.Date;
        if (rangeFrom > rangeTo) return BadRequest(new { message = "Report start date cannot be after the end date." });
        if ((rangeTo - rangeFrom).TotalDays > 3660) return BadRequest(new { message = "Report period cannot exceed ten years." });

        var userId = salesExecutiveId;
        var endExclusive = rangeTo.AddDays(1);
        var profile = await db.Users.Where(x => x.Id == userId && x.Role.Name == "SalesExecutive").Select(x => new { x.FullName, x.Email }).SingleOrDefaultAsync();
        if (profile is null) return NotFound(new { message = "Sales executive not found." });
        var leads = await db.Leads.Where(x => x.AssignedToId == userId && x.CreatedAt < endExclusive)
            .Select(x => new { x.Status, x.CreatedAt }).ToListAsync();
        var bookings = await db.Customers.Where(x => x.BookedById == userId && x.BookedAt >= rangeFrom && x.BookedAt < endExclusive)
            .Select(x => x.BookedAt!.Value).ToListAsync();
        var payments = await db.Payments.Where(x => x.SalesExecutiveId == userId && x.Status == PaymentStatus.Approved && !x.IsReversed && x.PaymentDate >= rangeFrom && x.PaymentDate < endExclusive)
            .Select(x => new { x.PaymentDate, x.Amount }).ToListAsync();
        var targets = await db.MonthlySalesTargets.Where(x => x.SalesExecutiveId == userId &&
                x.Month >= DateOnly.FromDateTime(new DateTime(rangeFrom.Year, rangeFrom.Month, 1)) &&
                x.Month <= DateOnly.FromDateTime(new DateTime(rangeTo.Year, rangeTo.Month, 1)))
            .ToDictionaryAsync(x => x.Month);

        var months = new List<object>();
        for (var cursor = new DateTime(rangeFrom.Year, rangeFrom.Month, 1); cursor <= rangeTo; cursor = cursor.AddMonths(1))
        {
            var monthEnd = cursor.AddMonths(1);
            var effectiveStart = cursor < rangeFrom ? rangeFrom : cursor;
            var effectiveEnd = monthEnd > endExclusive ? endExclusive : monthEnd;
            var monthLeads = leads.Where(x => x.CreatedAt >= effectiveStart && x.CreatedAt < effectiveEnd).ToList();
            var statusCounts = Enum.GetValues<LeadStatus>().ToDictionary(status => status.ToString(), status => monthLeads.Count(x => x.Status == status));
            var wins = bookings.Count(x => x >= effectiveStart && x < effectiveEnd);
            var lost = monthLeads.Count(x => x.Status == LeadStatus.Lost);
            var collection = payments.Where(x => x.PaymentDate >= effectiveStart && x.PaymentDate < effectiveEnd).Sum(x => x.Amount);
            targets.TryGetValue(DateOnly.FromDateTime(cursor), out var target);
            var unitTarget = target?.MinimumSalesUnits ?? 0;
            var collectionTarget = target?.MinimumCollectionAmount ?? 0;
            months.Add(new { month = DateOnly.FromDateTime(cursor), wins, lost, statusCounts, unitTarget, unitsAchieved = wins,
                unitVariance = wins - unitTarget, collectionTarget, collectionAchieved = collection,
                collectionVariance = collection - collectionTarget });
        }

        return Ok(new { employee = profile, from = rangeFrom, to = rangeTo, generatedAt = DateTime.UtcNow, months });
    }

    [HttpGet("targets")]
    [Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> GetTargetHistory()
    {
        var userId = User.UserId();
        var targets = await db.MonthlySalesTargets.Where(x => x.SalesExecutiveId == userId).OrderByDescending(x => x.Month).ToListAsync();
        var rows = new List<object>();
        foreach (var target in targets)
        {
            var start = target.Month.ToDateTime(TimeOnly.MinValue);
            var end = start.AddMonths(1);
            var units = await db.Customers.CountAsync(x => x.BookedById == userId && x.BookedAt >= start && x.BookedAt < end);
            var amount = await db.Payments.Where(x => x.SalesExecutiveId == userId && x.Status == PaymentStatus.Approved && !x.IsReversed && x.PaymentDate >= start && x.PaymentDate < end).SumAsync(x => (decimal?)x.Amount) ?? 0;
            rows.Add(new { target.Month, salesUnitTarget = target.MinimumSalesUnits, salesUnitsAchieved = units, salesUnitVariance = units - target.MinimumSalesUnits, collectionTarget = target.MinimumCollectionAmount, collectionAchieved = amount, collectionVariance = amount - target.MinimumCollectionAmount });
        }
        return Ok(rows);
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var today = DateTime.UtcNow.Date;
        var rangeFrom = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var rangeTo = (to ?? today).Date;
        if (rangeFrom > rangeTo) return BadRequest(new { message = "Dashboard start date cannot be after the end date." });
        var endExclusive = rangeTo.AddDays(1);

        if (User.IsInRole("VehicleDepartment"))
            return Ok(new
            {
                leads = await db.VehicleBookings.CountAsync(x => x.CreatedAt >= rangeFrom && x.CreatedAt < endExclusive),
                customers = await db.VehicleBookings.CountAsync(x => x.Status == VehicleBookingStatus.Approved && x.VisitDate >= DateOnly.FromDateTime(rangeFrom) && x.VisitDate <= DateOnly.FromDateTime(rangeTo)),
                projects = await db.Vehicles.CountAsync(x => x.IsActive),
                collectionFrom = rangeFrom, collectionTo = rangeTo
            });

        var customerIds = User.IsInRole("SalesExecutive")
            ? await db.Customers.Where(x => x.AssignedToId == User.UserId()).Select(x => x.Id).ToListAsync()
            : await db.Customers.Where(x => x.CreatedAt >= rangeFrom && x.CreatedAt < endExclusive).Select(x => x.Id).ToListAsync();
        var summaries = new List<FinancialSummary>();
        foreach (var id in customerIds) summaries.Add(await financial.SummaryAsync(id));
        if (User.IsInRole("SalesExecutive"))
        {
            var userId = User.UserId();
            var month = new DateOnly(today.Year, today.Month, 1);
            var monthStart = month.ToDateTime(TimeOnly.MinValue);
            var monthEnd = monthStart.AddMonths(1);
            var target = await db.MonthlySalesTargets.FirstOrDefaultAsync(x => x.SalesExecutiveId == userId && x.Month == month);
            var unitTarget = target?.MinimumSalesUnits ?? 0;
            var collectionTarget = target?.MinimumCollectionAmount ?? 0;
            var units = await db.Customers.CountAsync(x => x.BookedById == userId && x.BookedAt >= monthStart && x.BookedAt < monthEnd);
            var collection = await db.Payments.Where(x => x.SalesExecutiveId == userId && x.Status == PaymentStatus.Approved && !x.IsReversed && x.PaymentDate >= monthStart && x.PaymentDate < monthEnd).SumAsync(x => (decimal?)x.Amount) ?? 0;
            return Ok(new { assignedCustomers = customerIds.Count, customersWithCurrentDues = summaries.Count(x => x.CurrentDue > 0), customersWithOverduePayments = summaries.Count(x => x.OverdueAmount > 0), totalOutstanding = summaries.Sum(x => x.OutstandingBalance), upcomingEmiReminders = summaries.Count(x => x.NextEmiDueDate.HasValue), currentTarget = new { month, salesUnitTarget = unitTarget, salesUnitsAchieved = units, salesUnitVariance = units - unitTarget, collectionTarget, collectionAchieved = collection, collectionVariance = collection - collectionTarget } });
        }

        if (User.IsInRole("SubAdmin") || User.IsInRole("CS"))
            return Ok(new
            {
                leads = User.IsInRole("SubAdmin") ? await db.Leads.CountAsync(x => x.CreatedAt >= rangeFrom && x.CreatedAt < endExclusive) : 0,
                customers = customerIds.Count,
                projects = User.IsInRole("CS") ? await db.FinancialAgreements.CountAsync(x => x.CreatedAt >= rangeFrom && x.CreatedAt < endExclusive) : 0,
                collectionFrom = rangeFrom, collectionTo = rangeTo
            });

        var approvedInRange = db.Payments.Where(x => x.Status == PaymentStatus.Approved && !x.IsReversed && x.PaymentDate >= rangeFrom && x.PaymentDate < endExclusive);
        var pendingInRange = db.Payments.Where(x => x.Status == PaymentStatus.Pending && !x.IsReversed && x.PaymentDate >= rangeFrom && x.PaymentDate < endExclusive);
        var commissionsInRange = db.Commissions.Where(x => x.Status != CommissionStatus.Rejected && x.CreatedAt >= rangeFrom && x.CreatedAt < endExclusive);

        return Ok(new
        {
            leads = await db.Leads.CountAsync(x => x.CreatedAt >= rangeFrom && x.CreatedAt < endExclusive),
            customers = customerIds.Count,
            projects = await db.Projects.CountAsync(x => x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.SoldOut && x.Status != ProjectStatus.Paused),
            totalCollectible = summaries.Sum(x => x.TotalAgreedAmount),
            totalCollected = summaries.Sum(x => x.TotalPaid),
            totalCollection = await approvedInRange.SumAsync(x => (decimal?)x.Amount) ?? 0,
            collectionCount = await approvedInRange.CountAsync(),
            pendingCollection = await pendingInRange.SumAsync(x => (decimal?)x.Amount) ?? 0,
            totalCommission = await commissionsInRange.SumAsync(x => (decimal?)x.Amount) ?? 0,
            collectionFrom = rangeFrom,
            collectionTo = rangeTo,
            totalOutstanding = summaries.Sum(x => x.OutstandingBalance),
            totalDue = summaries.Sum(x => x.CurrentDue),
            totalOverdue = summaries.Sum(x => x.OverdueAmount),
            customersWithOverdueInstallments = summaries.Count(x => x.OverdueAmount > 0),
            pendingPayments = await pendingInRange.CountAsync(),
            approvedPayments = await approvedInRange.CountAsync()
        });
    }
}
