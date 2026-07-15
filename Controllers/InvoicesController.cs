using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
[Tags("Invoices")]
public class InvoicesController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetInvoices()
    {
        var query = db.Invoices.Include(x => x.Customer).Include(x => x.SalesExecutive).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.SalesExecutiveId == User.UserId());

        var invoices = await query.OrderByDescending(x => x.CreatedAt).Select(x => new
        {
            x.Id,
            x.InvoiceNumber,
            Customer = x.Customer.Name,
            SalesExecutive = x.SalesExecutive.FullName,
            x.FinalAmount,
            x.Status,
            x.DueDate
        }).ToListAsync();

        return Ok(invoices);
    }

    [HttpPost]
    public async Task<ActionResult> CreateInvoice(CreateInvoiceRequest request)
    {
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer is null) return BadRequest(new { message = "Customer not found." });

        var invoice = new Invoice
        {
            CustomerId = request.CustomerId,
            ProjectId = request.ProjectId,
            SalesExecutiveId = request.SalesExecutiveId ?? customer.AssignedToId ?? User.UserId(),
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
            DueDate = request.DueDate,
            Amount = request.Amount,
            Discount = request.Discount,
            Tax = request.Tax,
            FinalAmount = request.Amount - request.Discount + request.Tax,
            Status = InvoiceStatus.Generated,
            GeneratedById = User.UserId()
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return Created($"/api/invoices/{invoice.Id}", new { invoice.Id, invoice.InvoiceNumber, invoice.FinalAmount });
    }
}
