using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class LeadAutomationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<LeadAutomationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        await ProcessAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessAsync(stoppingToken);
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<IOneSignalNotificationService>();
            var settings = await db.LeadAutomationSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
                ?? new LeadAutomationSettings();
            var now = DateTime.UtcNow;

            var assigned = await db.Leads
                .Where(x => x.Status == LeadStatus.Assigned && x.AssignedToId != null)
                .ToListAsync(cancellationToken);

            foreach (var lead in assigned)
            {
                var assignedAt = lead.AssignedAt ?? lead.CreatedAt;
                if (lead.LastFollowUpAt == null && assignedAt.AddHours((double)settings.UnassignAfterHours) <= now)
                {
                    db.LeadReturns.Add(new LeadReturn
                    {
                        LeadId = lead.Id,
                        SalesExecutiveId = lead.AssignedToId!.Value,
                        AssignedAt = assignedAt,
                        ReturnedAt = now,
                        NotificationCount = lead.AssignmentReminderCount
                    });
                    lead.AssignedToId = null;
                    lead.Status = LeadStatus.New;
                    lead.AssignedAt = null;
                    lead.LastAssignmentReminderAt = null;
                    lead.AssignmentReminderCount = 0;
                    continue;
                }

                var reminderBase = lead.LastAssignmentReminderAt ?? assignedAt;
                if (reminderBase.AddHours((double)settings.ReminderIntervalHours) <= now)
                {
                    await notifier.SendLeadFollowUpReminderAsync(
                        lead.AssignedToId!.Value, lead.Id, lead.CustomerName, cancellationToken);
                    lead.LastAssignmentReminderAt = now;
                    lead.AssignmentReminderCount++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Lead automation cycle failed.");
        }
    }
}
