using backend.Models;

namespace backend.Dtos;

public record SubmitCollectionRequest(int CustomerId, decimal Amount, PaymentMethod Method, string? ProofUrl, string? Remarks);
public record RejectPaymentRequest(string Reason);
public record OnlinePaymentCallback(string InvoiceNumber, decimal Amount, string TransactionId);
