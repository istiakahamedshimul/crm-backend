using backend.Data;
using backend.Extensions;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/daily-work-reports"), Tags("Daily Work Reports")]
public class DailyWorkReportsController(CrmDbContext db) : ControllerBase
{
    [HttpPost, Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> Submit(SubmitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary)) return BadRequest(new { message = "Work summary is required." });
        if (request.Summary.Trim().Length > 5000) return BadRequest(new { message = "Work summary cannot exceed 5000 characters." });
        var workDate = request.WorkDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(6));
        var userId = User.UserId();
        var report = await db.DailyWorkReports.SingleOrDefaultAsync(x => x.SalesExecutiveId == userId && x.WorkDate == workDate);
        if (report is null)
        {
            report = new DailyWorkReport { SalesExecutiveId = userId, WorkDate = workDate };
            db.DailyWorkReports.Add(report);
        }
        report.Summary = request.Summary.Trim(); report.InputLanguage = request.InputLanguage?.Trim() ?? "bn_BD"; report.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { report.Id, report.WorkDate, updated = report.CreatedAt != report.UpdatedAt });
    }

    [HttpGet("mine"), Authorize(Roles = "SalesExecutive")]
    public async Task<ActionResult> Mine() => Ok(await db.DailyWorkReports.Where(x => x.SalesExecutiveId == User.UserId()).OrderByDescending(x => x.WorkDate).Take(30).Select(x => new { x.Id, x.WorkDate, x.Summary, x.InputLanguage, x.UpdatedAt }).ToListAsync());

    public record SubmitRequest(DateOnly? WorkDate, string Summary, string? InputLanguage);
}
