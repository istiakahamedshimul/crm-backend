using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PaymentService(CrmDbContext db) : IPaymentService
{
    public async Task ApplyInvoiceAndCommissionAsync(Payment payment)
    {
        if (payment.Amount <= 0) throw new InvalidOperationException("Collection amount must be greater than zero.");
        if (payment.InvoiceId.HasValue)
        {
            var invoice = await db.Invoices.FirstAsync(x => x.Id == payment.InvoiceId.Value);
            var approvedAmount = await db.Payments
                .Where(x => x.InvoiceId == invoice.Id && x.Status == PaymentStatus.Approved && x.Id != payment.Id)
                .SumAsync(x => x.Amount) + payment.Amount;
            invoice.Status = approvedAmount >= invoice.FinalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        }

        var customer = await db.Customers.FindAsync(payment.CustomerId);
        if (customer is not null) customer.PaymentStatus = "Positive";
        const decimal percentage = 7m;
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
