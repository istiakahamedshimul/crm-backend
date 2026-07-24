using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
[Tags("Payments")]
public class PaymentsController(CrmDbContext db, IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetPayments()
    {
        var query = db.Payments.Include(x => x.Customer).Include(x => x.SalesExecutive).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.SalesExecutiveId == User.UserId());

        var payments = await query.OrderByDescending(x => x.CreatedAt).Select(x => new
        {
            x.Id,
            Customer = x.Customer.Name,
            x.CollectionNumber,
            SalesExecutive = x.SalesExecutive.FullName,
            Amount = x.Status == PaymentStatus.Rejected ? -x.Amount : x.Amount,
            x.Method,
            x.Status,
            x.ProofUrl,
            x.RejectReason
        }).ToListAsync();

        return Ok(payments);
    }

    [HttpPost("collection")]
    public async Task<ActionResult> SubmitCollection(SubmitCollectionRequest request)
    {
        if (request.Amount <= 0) return BadRequest(new { message = "Collection amount must be greater than zero." });
        if (string.IsNullOrWhiteSpace(request.ProofUrl)) return BadRequest(new { message = "A receipt is required." });
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer is null) return BadRequest(new { message = "Booked customer not found." });
        if (!customer.LeadId.HasValue || !await db.Leads.AnyAsync(x => x.Id == customer.LeadId && x.Status == LeadStatus.Booked))
            return BadRequest(new { message = "Collections can only be submitted for customers with Booked lead status." });
        if (User.IsInRole("SalesExecutive") && customer.AssignedToId != User.UserId()) return Forbid();

        var payment = new Payment
        {
            CustomerId = customer.Id,
            CollectionNumber = $"COL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            SalesExecutiveId = User.UserId(),
            Amount = request.Amount,
            Method = request.Method,
            ProofUrl = request.ProofUrl,
            Remarks = request.Remarks,
            Status = PaymentStatus.Pending,
            SubmittedById = User.UserId()
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return Created($"/api/payments/{payment.Id}", new { payment.Id });
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult> ApprovePayment(int id)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null) return NotFound();
        if (payment.Amount <= 0) return BadRequest(new { message = "Collection amount must be greater than zero." });
        if (payment.Status == PaymentStatus.Approved) return Ok(new { message = "Already approved." });
        if (payment.Status == PaymentStatus.Rejected)
            return Conflict(new { message = "A rejected collection is final and cannot be approved again." });

        payment.Status = PaymentStatus.Approved;
        payment.VerifiedById = User.UserId();
        payment.VerifiedAt = DateTime.UtcNow;
        await paymentService.ApplyInvoiceAndCommissionAsync(payment);
        await db.SaveChangesAsync();

        return Ok(new { message = "Payment approved." });
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult> RejectPayment(int id, RejectPaymentRequest request)
    {
        var payment = await db.Payments.FindAsync(id);
        if (payment is null) return NotFound();
        if (payment.Status == PaymentStatus.Rejected)
            return Conflict(new { message = "This collection was already rejected and cannot be changed." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "A rejection reason is required." });

        payment.Status = PaymentStatus.Rejected;
        payment.VerifiedById = User.UserId();
        payment.VerifiedAt = DateTime.UtcNow;
        payment.RejectReason = request.Reason.Trim();

        var commissions = await db.Commissions.Where(x => x.PaymentId == payment.Id).ToListAsync();
        foreach (var commission in commissions) commission.Status = CommissionStatus.Rejected;

        if (payment.InvoiceId.HasValue)
        {
            var invoice = await db.Invoices.FindAsync(payment.InvoiceId.Value);
            if (invoice is not null)
            {
                var remainingApproved = await db.Payments
                    .Where(x => x.InvoiceId == invoice.Id &&
                                x.Status == PaymentStatus.Approved &&
                                x.Id != payment.Id)
                    .SumAsync(x => x.Amount);
                invoice.Status = remainingApproved <= 0
                    ? InvoiceStatus.Generated
                    : remainingApproved >= invoice.FinalAmount
                        ? InvoiceStatus.Paid
                        : InvoiceStatus.PartiallyPaid;
            }
        }

        var customer = await db.Customers.FindAsync(payment.CustomerId);
        if (customer is not null)
        {
            var hasOtherApprovedCollection = await db.Payments.AnyAsync(x =>
                x.CustomerId == payment.CustomerId &&
                x.Status == PaymentStatus.Approved &&
                x.Id != payment.Id);
            customer.PaymentStatus = hasOtherApprovedCollection ? "Positive" : "Unpaid";
        }
        await db.SaveChangesAsync();

        return Ok(new { message = "Payment rejected." });
    }

    [HttpPost("online-callback")]
    [AllowAnonymous]
    public async Task<ActionResult> OnlineCallback(OnlinePaymentCallback request)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.InvoiceNumber == request.InvoiceNumber);
        if (invoice is null) return NotFound();

        var payment = new Payment
        {
            CustomerId = invoice.CustomerId,
            InvoiceId = invoice.Id,
            CollectionNumber = $"COL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            SalesExecutiveId = invoice.SalesExecutiveId,
            Amount = request.Amount,
            Method = PaymentMethod.OnlineGateway,
            GatewayTransactionId = request.TransactionId,
            Status = PaymentStatus.Approved,
            VerifiedAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);
        await paymentService.ApplyInvoiceAndCommissionAsync(payment);
        await db.SaveChangesAsync();

        return Ok(new { message = "Online payment recorded." });
    }
}
