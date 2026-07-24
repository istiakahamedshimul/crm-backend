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
[Route("api/followups")]
[Tags("Follow-ups")]
public class FollowUpsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetFollowUps()
    {
        var query = db.FollowUps.Include(x => x.Lead).Include(x => x.Customer).Include(x => x.CreatedBy).Include(x => x.Proofs).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.CreatedById == User.UserId());

        var followUps = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.LeadId,
                x.CustomerId,
                Customer = x.Customer == null ? x.Lead.CustomerName : x.Customer.Name,
                Lead = x.Lead.CustomerName,
                SalesExecutive = x.CreatedBy.FullName,
                x.Type,
                x.Summary,
                x.CustomerResponse,
                x.NextFollowUpAt,
                x.CreatedAt,
                Proofs = x.Proofs.Select(p => new { p.ProofType, p.FileUrl })
            })
            .ToListAsync();

        return Ok(followUps);
    }

    [HttpPost]
    public async Task<ActionResult> CreateFollowUp(CreateFollowUpRequest request)
    {
        var lead = await db.Leads.FindAsync(request.LeadId);
        if (lead is null) return BadRequest(new { message = "Lead not found." });
        if (User.IsInRole("SalesExecutive") && lead.AssignedToId != User.UserId()) return Forbid();

        var followUp = new FollowUp
        {
            LeadId = request.LeadId,
            CustomerId = request.CustomerId,
            Type = request.Type,
            Summary = request.Summary,
            CustomerResponse = request.CustomerResponse,
            NextFollowUpAt = request.NextFollowUpAt,
            CreatedById = User.UserId()
        };

        db.FollowUps.Add(followUp);
        lead.Status = request.NewLeadStatus ?? lead.Status;
        lead.LastFollowUpAt = DateTime.UtcNow;
        lead.NextFollowUpAt = request.NextFollowUpAt;
        if (lead.Status == LeadStatus.Booked && !await db.Customers.AnyAsync(x => x.LeadId == lead.Id))
        {
            db.Customers.Add(new Customer
            {
                LeadId = lead.Id, Name = lead.CustomerName, Phone = lead.Phone,
                AlternativePhone = lead.AlternativePhone, Email = lead.Email, Address = lead.Address,
                AssignedToId = lead.AssignedToId, ProjectId = lead.ProjectId,
                PaymentStatus = "Positive"
            });
        }
        await db.SaveChangesAsync();

        foreach (var proof in request.Proofs ?? [])
        {
            db.FollowUpProofs.Add(new FollowUpProof
            {
                FollowUpId = followUp.Id,
                ProofType = proof.ProofType,
                FileUrl = proof.FileUrl
            });
        }

        await db.SaveChangesAsync();
        return Created($"/api/followups/{followUp.Id}", new { followUp.Id });
    }
}
