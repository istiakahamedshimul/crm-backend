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
            x.Amount,
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

        payment.Status = PaymentStatus.Rejected;
        payment.VerifiedById = User.UserId();
        payment.VerifiedAt = DateTime.UtcNow;
        payment.RejectReason = request.Reason;
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
