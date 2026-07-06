using backend.Models;

namespace backend.Services;

public interface IPaymentService
{
    Task ApplyInvoiceAndCommissionAsync(Payment payment);
}
