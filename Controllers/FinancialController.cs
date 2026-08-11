using System.Text.Json;
using backend.Data;
using backend.Extensions;
using backend.Models;
using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/customers/{customerId:int}/financial"), Tags("Customer Financials")]
public class FinancialController(CrmDbContext db, IFinancialService financial) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult> Summary(int customerId)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return NotFound();
        if (User.IsInRole("SalesExecutive") && customer.AssignedToId != User.UserId()) return Forbid();
        return Ok(await financial.SummaryAsync(customerId));
    }

    [HttpGet("history")]
    public async Task<ActionResult> History(int customerId)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return NotFound();
        if (User.IsInRole("SalesExecutive") && customer.AssignedToId != User.UserId()) return Forbid();

        var agreement = await db.FinancialAgreements.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => new
            {
                x.Id, x.CustomerId, x.TotalAgreedAmount, x.BookingAmount, x.DownPaymentAmount,
                x.PaymentPlan, x.EmiStartDate, x.MonthlyEmiAmount, x.InstallmentCount,
                x.Remarks, x.CreatedById, x.UpdatedById, x.CreatedAt, x.UpdatedAt
            }).SingleOrDefaultAsync();
        var installments = await db.EmiInstallments.AsNoTracking()
            .Where(x => x.FinancialAgreement.CustomerId == customerId)
            .OrderBy(x => x.InstallmentNumber)
            .Select(x => new { x.Id, x.FinancialAgreementId, x.InstallmentNumber, x.DueDate, x.ExpectedAmount, x.PaidAmount, x.Status, x.PaidAt })
            .ToListAsync();
        var payments = await db.Payments.AsNoTracking().Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Amount, x.PaymentDate, x.Method, x.Purpose, x.InstallmentId, x.TransactionReference, x.ProofUrl, x.Status, x.IsReversed, x.ReversalReason, x.CreatedAt })
            .ToListAsync();
        var audit = await db.FinancialAuditLogs.AsNoTracking().Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Action, x.DetailsJson, x.PerformedById, x.CreatedAt })
            .ToListAsync();
        return Ok(new { agreement, installments, payments, audit });
    }

    [HttpPut("agreement"), RequirePermission(PermissionCodes.AgreementsManage)]
    public async Task<ActionResult> Agreement(int customerId, AgreementRequest request)
    {
        if (request.TotalAgreedAmount <= 0 || request.BookingAmount < 0 || request.DownPaymentAmount < 0 ||
            request.BookingAmount + request.DownPaymentAmount > request.TotalAgreedAmount)
            return BadRequest(new { message = "Agreement, booking, or down-payment amounts are invalid." });
        if (request.PaymentPlan == PaymentPlanType.Emi &&
            (request.EmiStartDate is null || request.MonthlyEmiAmount is null or <= 0 || request.InstallmentCount is null or <= 0))
            return BadRequest(new { message = "EMI start date, monthly amount and installment count are required." });

        if (!await db.Customers.AnyAsync(x => x.Id == customerId)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var agreement = await db.FinancialAgreements.Include(x => x.Installments).SingleOrDefaultAsync(x => x.CustomerId == customerId);
        var action = agreement is null ? FinancialAuditAction.AgreementCreated : FinancialAuditAction.AgreementUpdated;
        if (agreement is null)
        {
            agreement = new FinancialAgreement { CustomerId = customerId, CreatedById = User.UserId(), UpdatedById = User.UserId() };
            db.Add(agreement);
        }

        agreement.TotalAgreedAmount = request.TotalAgreedAmount;
        agreement.BookingAmount = request.BookingAmount;
        agreement.DownPaymentAmount = request.PaymentPlan == PaymentPlanType.Emi ? request.DownPaymentAmount : 0;
        agreement.PaymentPlan = request.PaymentPlan;
        agreement.EmiStartDate = request.EmiStartDate?.Date;
        agreement.MonthlyEmiAmount = request.MonthlyEmiAmount;
        agreement.InstallmentCount = request.InstallmentCount;
        agreement.Remarks = request.Remarks;
        agreement.UpdatedById = User.UserId();
        agreement.UpdatedAt = DateTime.UtcNow;

        if (agreement.Installments.Count > 0 && !await db.Payments.AnyAsync(x => x.CustomerId == customerId && x.Status == PaymentStatus.Approved && !x.IsReversed))
            db.EmiInstallments.RemoveRange(agreement.Installments);
        else if (agreement.Installments.Count > 0)
            return Conflict(new { message = "An agreement with payment history cannot regenerate its schedule." });

        var scheduleBalance = request.TotalAgreedAmount - request.BookingAmount - agreement.DownPaymentAmount;
        if (request.PaymentPlan == PaymentPlanType.Emi)
        {
            var remaining = scheduleBalance;
            for (var number = 1; number <= request.InstallmentCount!.Value && remaining > 0; number++)
            {
                var amount = Math.Min(request.MonthlyEmiAmount!.Value, remaining);
                agreement.Installments.Add(new EmiInstallment { InstallmentNumber = number, DueDate = request.EmiStartDate!.Value.Date.AddMonths(number - 1), ExpectedAmount = amount, Status = InstallmentStatus.Upcoming });
                remaining -= amount;
            }
            if (remaining > 0) return BadRequest(new { message = "EMI duration and amount do not cover the balance after booking and down payment." });
        }
        else if (scheduleBalance > 0)
        {
            agreement.Installments.Add(new EmiInstallment { InstallmentNumber = 1, DueDate = request.EmiStartDate?.Date ?? DateTime.UtcNow.Date, ExpectedAmount = scheduleBalance, Status = InstallmentStatus.Upcoming });
        }

        db.FinancialAuditLogs.Add(new FinancialAuditLog { CustomerId = customerId, Action = action, DetailsJson = JsonSerializer.Serialize(request), PerformedById = User.UserId() });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(await financial.SummaryAsync(customerId));
    }

    public record AgreementRequest(decimal TotalAgreedAmount, decimal BookingAmount, decimal DownPaymentAmount,
        PaymentPlanType PaymentPlan, DateTime? EmiStartDate, decimal? MonthlyEmiAmount, int? InstallmentCount, string? Remarks);
}
