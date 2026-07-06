using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PaymentService(CrmDbContext db) : IPaymentService
{
    public async Task ApplyInvoiceAndCommissionAsync(Payment payment)
    {
        var invoice = await db.Invoices.FirstAsync(x => x.Id == payment.InvoiceId);
        var approvedAmount = await db.Payments
            .Where(x => x.InvoiceId == invoice.Id && x.Status == PaymentStatus.Approved)
            .SumAsync(x => x.Amount);

        invoice.Status = approvedAmount >= invoice.FinalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        var customer = await db.Customers.FindAsync(invoice.CustomerId);
        if (customer is not null)
        {
            customer.PaymentStatus = invoice.Status == InvoiceStatus.Paid ? "Paid" : "Partial";
        }

        var rule = await db.CommissionRules.FirstOrDefaultAsync(x => x.IsActive);
        var percentage = rule?.Percentage ?? 2m;
        db.Commissions.Add(new Commission
        {
            SalesExecutiveId = payment.SalesExecutiveId,
            CustomerId = payment.CustomerId,
            InvoiceId = payment.InvoiceId,
            Payment = payment,
            PaymentAmount = payment.Amount,
            Percentage = percentage,
            Amount = Math.Round(payment.Amount * percentage / 100m, 2),
            Status = CommissionStatus.Pending
        });
    }
}
