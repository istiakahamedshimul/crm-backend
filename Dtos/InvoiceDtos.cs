namespace backend.Dtos;

public record CreateInvoiceRequest(int CustomerId, int? ProjectId, int? SalesExecutiveId, DateTime DueDate, decimal Amount, decimal Discount, decimal Tax);
