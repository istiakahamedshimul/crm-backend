using System.Net.Http.Headers;
using System.Net.Http.Json;
using backend.Options;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class OneSignalNotificationService(
    HttpClient httpClient,
    IOptions<OneSignalOptions> options,
    CrmDbContext db,
    ILogger<OneSignalNotificationService> logger) : IOneSignalNotificationService
{
    private readonly OneSignalOptions settings = options.Value;

    public async Task SendLeadAssignedAsync(
        int salesExecutiveId,
        int leadId,
        string customerName,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(salesExecutiveId, leadId, customerName, "LeadAssigned", "New lead assigned",
            $"New lead assigned: {customerName}", cancellationToken);
    }

    public async Task SendLeadFollowUpReminderAsync(
        int salesExecutiveId, int leadId, string customerName, CancellationToken cancellationToken = default)
    {
        await SendAsync(salesExecutiveId, leadId, customerName, "LeadFollowUpOverdue", "Lead follow-up overdue",
            $"Please follow up with {customerName} as soon as possible. If no follow-up activity is recorded, this lead will be automatically returned to the admin queue for reassignment.",
            cancellationToken);
    }

    public async Task SendFinancialPushAsync(int salesExecutiveId, int customerId, string title, string message, CancellationToken cancellationToken = default)
    {
        await SendPushAsync(salesExecutiveId, title, message, new { screen = "customer", customerId }, cancellationToken);
    }

    private async Task SendAsync(int salesExecutiveId, int leadId, string customerName, string type, string title, string message,
        CancellationToken cancellationToken)
    {
        var eventKey = $"{type}:lead:{leadId}";
        if (!await db.AppNotifications.AnyAsync(x => x.UserId == salesExecutiveId && x.EventKey == eventKey, cancellationToken))
        {
            db.AppNotifications.Add(new AppNotification
            {
                UserId = salesExecutiveId, Title = title, Message = message, Type = type,
                Screen = "assigned_leads", LeadId = leadId, EventKey = eventKey
            });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { db.ChangeTracker.Clear(); }
        }

        await SendPushAsync(salesExecutiveId, title, message, new { screen = "assigned_leads", leadId }, cancellationToken);
    }

    private async Task SendPushAsync(int salesExecutiveId, string title, string message, object data, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("OneSignal notification skipped because AppId or ApiKey is not configured.");
            return;
        }

        var payload = new
        {
            app_id = settings.AppId,
            headings = new { en = title },
            contents = new { en = message },
            // Android plays this bundled ten-second tone even when Flutter is not running.
            android_sound = "lead_notification",
            existing_android_channel_id = "crm_warning_alerts_v1",
            include_aliases = new Dictionary<string, string[]>
            {
                ["external_id"] = [$"crm-user-{salesExecutiveId}"]
            },
            target_channel = "push",
            data
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "notifications?c=push")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Key", settings.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "OneSignal rejected notification with status {StatusCode}: {ResponseBody}", (int)response.StatusCode, responseBody);
            }
        }
        catch (Exception exception)
        {
            // Lead assignment must remain successful if the external push service is unavailable.
            logger.LogError(exception, "Could not send OneSignal notification.");
        }
    }
}
