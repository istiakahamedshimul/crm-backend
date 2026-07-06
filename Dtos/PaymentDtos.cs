using backend.Models;

namespace backend.Dtos;

public record SubmitPaymentRequest(int InvoiceId, decimal Amount, PaymentMethod Method, string? ProofUrl, string? Remarks);
public record RejectPaymentRequest(string Reason);
public record OnlinePaymentCallback(string InvoiceNumber, decimal Amount, string TransactionId);
