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
[Route("api/leads")]
[Tags("Leads")]
public class LeadsController(
    CrmDbContext db,
    ILeadAssignmentService assignmentService,
    IOneSignalNotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LeadDto>>> GetLeads()
    {
        var query = db.Leads.Include(x => x.AssignedTo).Include(x => x.Project).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.AssignedToId == User.UserId());

        var leads = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadDto(x.Id, x.CustomerName, x.Phone, x.Email, x.Source, x.Priority, x.Status, x.AssignedToId, x.AssignedTo == null ? null : x.AssignedTo.FullName, x.ProjectId, x.Project == null ? null : x.Project.Name, x.NextFollowUpAt))
            .ToListAsync();

        return Ok(leads);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateLead(CreateLeadRequest request)
    {
        if (!request.AssignedToId.HasValue)
        {
            return BadRequest(new { message = "Lead must be assigned to a sales executive by admin." });
        }

        var salesExecutive = await assignmentService.GetActiveSalesExecutiveAsync(request.AssignedToId.Value);
        if (salesExecutive is null)
        {
            return BadRequest(new { message = "Assigned user must be an active sales executive." });
        }

        if (request.ProjectId.HasValue && !await db.Projects.AnyAsync(x => x.Id == request.ProjectId.Value))
        {
            return BadRequest(new { message = "Selected project was not found." });
        }

        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await db.Customers.FindAsync(request.CustomerId.Value);
            if (customer is null) return BadRequest(new { message = "Selected customer was not found." });

            var hasActiveAssignment = await db.Leads.AnyAsync(lead =>
                lead.Status != LeadStatus.Booked &&
                (lead.Phone == customer.Phone ||
                 (customer.Email != null && lead.Email != null && lead.Email.ToLower() == customer.Email.ToLower())));
            if (hasActiveAssignment)
                return Conflict(new { message = "This customer is already assigned. They can be assigned again after the current lead is Booked." });
        }

        var customerName = customer?.Name ?? request.CustomerName;
        var phone = customer?.Phone ?? request.Phone;
        var alternativePhone = customer?.AlternativePhone ?? request.AlternativePhone;
        var email = customer?.Email ?? request.Email;
        var address = customer?.Address ?? request.Address;

        var conflict = await assignmentService.GetAssignmentConflictAsync(
            phone, email, request.AssignedToId.Value, ignoreCustomerId: customer?.Id);
        if (conflict is not null)
        {
            return Conflict(new { message = conflict });
        }

        var lead = new Lead
        {
            CustomerName = customerName,
            Phone = phone,
            AlternativePhone = alternativePhone,
            Email = email,
            Address = address,
            BudgetRange = request.BudgetRange,
            PreferredLocation = request.PreferredLocation,
            ProjectId = request.ProjectId,
            Source = request.Source,
            Priority = request.Priority,
            Status = LeadStatus.Assigned,
            AssignedToId = request.AssignedToId,
            CreatedById = User.UserId(),
            Remarks = request.Remarks
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        await notificationService.SendLeadAssignedAsync(
            request.AssignedToId.Value, lead.Id, lead.CustomerName, HttpContext.RequestAborted);
        return Created($"/api/leads/{lead.Id}", new { lead.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateLead(int id, UpdateLeadRequest request)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();
        if (User.IsInRole("SalesExecutive") && lead.AssignedToId != User.UserId()) return Forbid();
        var previousAssignedToId = lead.AssignedToId;
        if (request.AssignedToId.HasValue)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var salesExecutive = await assignmentService.GetActiveSalesExecutiveAsync(request.AssignedToId.Value);
            if (salesExecutive is null)
            {
                return BadRequest(new { message = "Assigned user must be an active sales executive." });
            }

            var conflict = await assignmentService.GetAssignmentConflictAsync(lead.Phone, lead.Email, request.AssignedToId.Value, ignoreLeadId: lead.Id);
            if (conflict is not null)
            {
                return Conflict(new { message = conflict });
            }
        }

        lead.Status = request.Status ?? lead.Status;
        lead.Priority = request.Priority ?? lead.Priority;
        lead.AssignedToId = request.AssignedToId ?? lead.AssignedToId;
        lead.NextFollowUpAt = request.NextFollowUpAt ?? lead.NextFollowUpAt;
        lead.Remarks = request.Remarks ?? lead.Remarks;

        await db.SaveChangesAsync();
        if (request.AssignedToId.HasValue && request.AssignedToId != previousAssignedToId)
        {
            await notificationService.SendLeadAssignedAsync(
                request.AssignedToId.Value, lead.Id, lead.CustomerName, HttpContext.RequestAborted);
        }
        return NoContent();
    }
}
