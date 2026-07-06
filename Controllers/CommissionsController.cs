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
[Route("api/commissions")]
[Tags("Commissions")]
public class CommissionsController(CrmDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> GetMyCommission()
    {
        var userId = User.UserId();
        var rows = await db.Commissions.Include(x => x.Payment)
            .Where(x => x.SalesExecutiveId == userId)
            .ToListAsync();

        return Ok(new
        {
            totalEarned = rows.Sum(x => x.Amount),
            pending = rows.Where(x => x.Status == CommissionStatus.Pending).Sum(x => x.Amount),
            paid = rows.Where(x => x.Status == CommissionStatus.Paid).Sum(x => x.Amount),
            history = rows.OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.PaymentId, x.PaymentAmount, x.Percentage, x.Amount, x.Status, x.CreatedAt })
        });
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant,Manager")]
    public async Task<ActionResult> GetCommissions()
    {
        var commissions = await db.Commissions.Include(x => x.SalesExecutive).OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                SalesExecutive = x.SalesExecutive.FullName,
                x.PaymentId,
                x.PaymentAmount,
                x.Percentage,
                x.Amount,
                x.Status,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(commissions);
    }

    [HttpPost("/api/commission-rules")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateRule(CommissionRuleRequest request)
    {
        foreach (var rule in db.CommissionRules) rule.IsActive = false;

        var newRule = new CommissionRule
        {
            Name = request.Name,
            Percentage = request.Percentage,
            IsActive = true
        };

        db.CommissionRules.Add(newRule);
        await db.SaveChangesAsync();
        return Created($"/api/commission-rules/{newRule.Id}", new { newRule.Id });
    }
}
