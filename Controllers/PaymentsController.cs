using System.Text.Json;
using backend.Data; using backend.Extensions; using backend.Models; using backend.Security; using backend.Services;
using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace backend.Controllers;

[ApiController, Authorize, Route("api/payments"), Tags("Payments")]
public class PaymentsController(CrmDbContext db, IFinancialService financial) : ControllerBase
{
    [HttpGet, RequirePermission(PermissionCodes.PaymentsView)]
    public async Task<ActionResult> Get([FromQuery]int page=1,[FromQuery]int pageSize=50,[FromQuery]int? customerId=null,[FromQuery]int? salesExecutiveId=null,[FromQuery]PaymentStatus? status=null)
    {
        page=Math.Max(1,page); pageSize=Math.Clamp(pageSize,1,100);
        var query=db.Payments.Include(x=>x.Customer).Include(x=>x.SalesExecutive).AsQueryable();
        if(customerId.HasValue)query=query.Where(x=>x.CustomerId==customerId); if(salesExecutiveId.HasValue)query=query.Where(x=>x.SalesExecutiveId==salesExecutiveId); if(status.HasValue)query=query.Where(x=>x.Status==status);
        var total=await query.CountAsync();
        var items=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.CustomerId,Customer=x.Customer.Name,x.CollectionNumber,x.SalesExecutiveId,SalesExecutive=x.SalesExecutive.FullName,x.Amount,x.PaymentDate,x.Method,x.Purpose,x.TransactionReference,x.InstallmentId,x.Status,x.ProofUrl,x.Remarks,x.IsReversed,x.ReversalReason,x.CreatedAt}).ToListAsync();
        return Ok(new{items,total,page,pageSize});
    }

    [HttpPost, RequirePermission(PermissionCodes.PaymentsRecord)]
    public async Task<ActionResult> Record(RecordPaymentRequest request,[FromHeader(Name="Idempotency-Key")]string? key)
    {
        if(request.Amount<=0)return BadRequest(new{message="Payment amount must be greater than zero."});
        if(string.IsNullOrWhiteSpace(key))return BadRequest(new{message="Idempotency-Key header is required."});
        var existing=await db.Payments.SingleOrDefaultAsync(x=>x.IdempotencyKey==key); if(existing!=null)return Ok(new{existing.Id,duplicate=true});
        var customer=await db.Customers.Include(x=>x.FinancialAgreement).SingleOrDefaultAsync(x=>x.Id==request.CustomerId);
        if(customer?.FinancialAgreement is null)return BadRequest(new{message="Customer financial agreement is required."});
        if(customer.AssignedToId is null)return BadRequest(new{message="Customer must have an assigned Sales Executive."});
        var before=await financial.SummaryAsync(request.CustomerId); if(request.Amount>before.OutstandingBalance)return BadRequest(new{message="Payment exceeds outstanding balance."});
        EmiInstallment? installment=null;
        if(request.Purpose==PaymentPurpose.EmiInstallment)
        {
            if(!request.InstallmentId.HasValue)return BadRequest(new{message="Select the EMI installment month."});
            installment=await db.EmiInstallments.SingleOrDefaultAsync(x=>x.Id==request.InstallmentId&&x.FinancialAgreement.CustomerId==request.CustomerId);
            if(installment is null)return BadRequest(new{message="Installment does not belong to customer."});
            var remaining=installment.ExpectedAmount-installment.PaidAmount; if(remaining<=0)return Conflict(new{message="Selected EMI installment is already paid."});
        }
        else if(request.InstallmentId.HasValue)return BadRequest(new{message="An installment can only be selected for an EMI payment."});

        await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var payment=new Payment{CustomerId=request.CustomerId,CollectionNumber=$"COL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..31],SalesExecutiveId=customer.AssignedToId.Value,Amount=request.Amount,PaymentDate=request.PaymentDate.ToUniversalTime(),Method=request.Method,Purpose=request.Purpose,TransactionReference=request.TransactionReference?.Trim(),InstallmentId=request.InstallmentId,ProofUrl=request.ProofUrl,Remarks=request.Remarks,Status=PaymentStatus.Approved,SubmittedById=User.UserId(),VerifiedById=User.UserId(),VerifiedAt=DateTime.UtcNow,IdempotencyKey=key};
        db.Add(payment); db.FinancialAuditLogs.Add(new FinancialAuditLog{CustomerId=request.CustomerId,PaymentId=payment.Id,Action=FinancialAuditAction.PaymentRecorded,DetailsJson=JsonSerializer.Serialize(request),PerformedById=User.UserId()});
        try
        {
            await db.SaveChangesAsync(); await financial.RecalculateInstallmentsAsync(customer.FinancialAgreement.Id); var after=await financial.SummaryAsync(request.CustomerId);
            AddBalanceNotification(customer,after,$"balance:payment:{payment.Id}"); await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Created($"/api/payments/{payment.Id}",new{payment.Id,summary=after});
        }
        catch(DbUpdateException){await transaction.RollbackAsync();return Conflict(new{message="Duplicate or concurrent payment submission detected."});}
    }

    [HttpPost("{id:int}/reverse"),RequirePermission(PermissionCodes.PaymentsReverse)]
    public async Task<ActionResult> Reverse(int id,ReasonRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.Reason))return BadRequest(new{message="Reversal reason is required."}); await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var payment=await db.Payments.Include(x=>x.Customer).ThenInclude(x=>x.FinancialAgreement).SingleOrDefaultAsync(x=>x.Id==id); if(payment is null)return NotFound(); if(payment.IsReversed)return Conflict(new{message="Payment already reversed."});
        payment.IsReversed=true;payment.ReversedAt=DateTime.UtcNow;payment.ReversedById=User.UserId();payment.ReversalReason=request.Reason.Trim();db.FinancialAuditLogs.Add(new FinancialAuditLog{CustomerId=payment.CustomerId,PaymentId=payment.Id,Action=FinancialAuditAction.PaymentReversed,DetailsJson=JsonSerializer.Serialize(request),PerformedById=User.UserId()});
        await db.SaveChangesAsync();if(payment.Customer.FinancialAgreement!=null)await financial.RecalculateInstallmentsAsync(payment.Customer.FinancialAgreement.Id);var after=await financial.SummaryAsync(payment.CustomerId);AddBalanceNotification(payment.Customer,after,$"balance:reversal:{payment.Id}");await db.SaveChangesAsync();await transaction.CommitAsync();return Ok(after);
    }

    [HttpPost("{id:int}/approve"),RequirePermission(PermissionCodes.PaymentsApprove)]
    public async Task<ActionResult> Approve(int id){var payment=await db.Payments.Include(x=>x.Customer).ThenInclude(x=>x.FinancialAgreement).SingleOrDefaultAsync(x=>x.Id==id);if(payment is null)return NotFound();if(payment.Status==PaymentStatus.Approved)return Ok(new{message="Already approved."});if(payment.Status==PaymentStatus.Rejected||payment.IsReversed)return Conflict(new{message="Rejected or reversed payment cannot be approved."});var summary=await financial.SummaryAsync(payment.CustomerId);if(payment.Amount>summary.OutstandingBalance)return BadRequest(new{message="Payment exceeds outstanding balance."});payment.Status=PaymentStatus.Approved;payment.VerifiedById=User.UserId();payment.VerifiedAt=DateTime.UtcNow;db.FinancialAuditLogs.Add(new FinancialAuditLog{CustomerId=payment.CustomerId,PaymentId=payment.Id,Action=FinancialAuditAction.PaymentApproved,PerformedById=User.UserId()});await db.SaveChangesAsync();if(payment.Customer.FinancialAgreement!=null)await financial.RecalculateInstallmentsAsync(payment.Customer.FinancialAgreement.Id);return Ok(new{message="Payment approved."});}

    [HttpPost("{id:int}/reject"),RequirePermission(PermissionCodes.PaymentsApprove)]
    public async Task<ActionResult> Reject(int id,ReasonRequest request){if(string.IsNullOrWhiteSpace(request.Reason))return BadRequest(new{message="Reason is required."});var payment=await db.Payments.FindAsync(id);if(payment is null)return NotFound();if(payment.Status!=PaymentStatus.Pending)return Conflict(new{message="Only pending payments can be rejected."});payment.Status=PaymentStatus.Rejected;payment.RejectReason=request.Reason.Trim();payment.VerifiedById=User.UserId();payment.VerifiedAt=DateTime.UtcNow;db.FinancialAuditLogs.Add(new FinancialAuditLog{CustomerId=payment.CustomerId,PaymentId=payment.Id,Action=FinancialAuditAction.PaymentRejected,DetailsJson=JsonSerializer.Serialize(request),PerformedById=User.UserId()});await db.SaveChangesAsync();return Ok(new{message="Payment rejected."});}

    private void AddBalanceNotification(Customer customer,FinancialSummary summary,string eventKey){if(customer.AssignedToId is null)return;db.AppNotifications.Add(new AppNotification{UserId=customer.AssignedToId.Value,CustomerId=customer.Id,CustomerName=customer.Name,FileId=customer.FileId,Title="Customer balance updated",Message=$"{customer.Name}'s outstanding balance is now {summary.OutstandingBalance:0.00}.",Type="OutstandingBalanceChanged",Screen="customer",DueAmount=summary.CurrentDue,OutstandingBalance=summary.OutstandingBalance,EventKey=eventKey});}
    public record RecordPaymentRequest(int CustomerId,decimal Amount,DateTime PaymentDate,PaymentMethod Method,PaymentPurpose Purpose,string? TransactionReference,int? InstallmentId,string? ProofUrl,string? Remarks);
    public record ReasonRequest(string Reason);
}
