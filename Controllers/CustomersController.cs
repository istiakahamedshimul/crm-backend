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
[Route("api/customers")]
[Tags("Customers")]
public class CustomersController(CrmDbContext db, ILeadAssignmentService assignmentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetCustomers()
    {
        var query = db.Customers.Include(x => x.AssignedTo).Include(x => x.Project).ThenInclude(x => x!.SubGroup).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.AssignedToId == User.UserId());

        var customers = await query.Select(x => new
        {
            x.Id,
            x.LeadId,
            x.Name,
            x.Phone,
            x.Email,
            x.PaymentStatus,
            x.ProjectId,
            Project = x.Project == null ? null : x.Project.Name,
            ProjectType = x.Project == null ? (ProjectType?)null : x.Project.Type,
            SubGroupId = x.Project == null ? (int?)null : x.Project.SubGroupId,
            SubGroup = x.Project == null ? null : x.Project.SubGroup.Name,
            SalesExecutive = x.AssignedTo == null ? null : x.AssignedTo.FullName
        }).ToListAsync();

        return Ok(customers);
    }

    [HttpPut("{id:int}/project")]
    [Authorize(Roles = "SuperAdmin,Admin,SalesExecutive")]
    public async Task<ActionResult> UpdateProject(int id, UpdateCustomerProjectRequest request)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null) return NotFound(new { message = "Customer not found." });
        if (User.IsInRole("SalesExecutive") && customer.AssignedToId != User.UserId()) return Forbid();
        if (request.ProjectId.HasValue && !await db.Projects.AnyAsync(x => x.Id == request.ProjectId.Value))
            return BadRequest(new { message = "Project not found." });
        customer.ProjectId = request.ProjectId;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateCustomer(CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Customer name and phone are required." });

        Lead? lead = null;
        if (request.LeadId.HasValue)
        {
            lead = await db.Leads.FindAsync(request.LeadId.Value);
            if (lead is null) return BadRequest(new { message = "Lead not found." });
            if (await db.Customers.AnyAsync(x => x.LeadId == lead.Id)) return Conflict(new { message = "Customer profile already exists for this lead." });
        }

        var phone = request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (await db.Customers.AnyAsync(x => x.Phone == phone || (email != null && x.Email != null && x.Email.ToLower() == email.ToLower())))
            return Conflict(new { message = "A customer with this phone or email already exists." });

        var customer = new Customer
        {
            LeadId = lead?.Id,
            Name = request.Name.Trim(),
            Phone = phone,
            AlternativePhone = request.AlternativePhone,
            Email = email,
            Address = request.Address,
            Occupation = request.Occupation,
            NidOrPassport = request.NidOrPassport,
            NomineeName = request.NomineeName,
            NomineePhone = request.NomineePhone,
            AssignedToId = lead?.AssignedToId,
            ProjectId = lead?.ProjectId
        };

        if (lead is not null) lead.Status = LeadStatus.Booked;
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return Created($"/api/customers/{customer.Id}", new { customer.Id, customer.LeadId, customer.AssignedToId });
    }

    [HttpPost("from-lead/{leadId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateFromLead(int leadId, CreateCustomerFromLeadRequest request)
    {
        var lead = await db.Leads.FindAsync(leadId);
        if (lead is null) return NotFound();
        if (!lead.AssignedToId.HasValue) return BadRequest(new { message = "Lead must be assigned to a sales executive before creating a customer profile." });
        if (await db.Customers.AnyAsync(x => x.LeadId == lead.Id)) return Conflict(new { message = "Customer profile already exists for this lead." });

        var conflict = await assignmentService.GetAssignmentConflictAsync(lead.Phone, lead.Email, lead.AssignedToId.Value);
        if (conflict is not null) return Conflict(new { message = conflict });

        var customer = new Customer
        {
            LeadId = lead.Id,
            Name = lead.CustomerName,
            Phone = lead.Phone,
            AlternativePhone = lead.AlternativePhone,
            Email = lead.Email,
            Address = lead.Address,
            Occupation = request.Occupation,
            NidOrPassport = request.NidOrPassport,
            NomineeName = request.NomineeName,
            NomineePhone = request.NomineePhone,
            AssignedToId = lead.AssignedToId
        };

        lead.Status = LeadStatus.Booked;
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return Created($"/api/customers/{customer.Id}", new { customer.Id, customer.LeadId, customer.AssignedToId });
    }
}
