using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/lead-automation-settings")]
[Tags("Lead automation")]
public class LeadAutomationSettingsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LeadAutomationSettingsDto>> Get()
    {
        var settings = await GetOrCreateAsync();
        return Ok(new LeadAutomationSettingsDto(settings.UnassignAfterHours, settings.ReminderIntervalHours));
    }

    [HttpPut]
    public async Task<ActionResult<LeadAutomationSettingsDto>> Update(UpdateLeadAutomationSettingsRequest request)
    {
        const decimal oneMinuteInHours = 1m / 60m;
        if (request.UnassignAfterHours < oneMinuteInHours || request.UnassignAfterHours > 720 ||
            request.ReminderIntervalHours < oneMinuteInHours || request.ReminderIntervalHours > 168)
            return BadRequest(new { message = "Times may use fractional hours but cannot be shorter than one minute. Maximums are 720 return hours and 168 reminder hours." });

        var settings = await GetOrCreateAsync();
        settings.UnassignAfterHours = request.UnassignAfterHours;
        settings.ReminderIntervalHours = request.ReminderIntervalHours;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new LeadAutomationSettingsDto(settings.UnassignAfterHours, settings.ReminderIntervalHours));
    }

    private async Task<LeadAutomationSettings> GetOrCreateAsync()
    {
        var settings = await db.LeadAutomationSettings.SingleOrDefaultAsync(x => x.Id == 1);
        if (settings is not null) return settings;
        settings = new LeadAutomationSettings();
        db.LeadAutomationSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }
}
