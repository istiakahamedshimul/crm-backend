using System.Net.Http.Headers;
using System.Net.Http.Json;
using backend.Options;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class OneSignalNotificationService(
    HttpClient httpClient,
    IOptions<OneSignalOptions> options,
    ILogger<OneSignalNotificationService> logger) : IOneSignalNotificationService
{
    private readonly OneSignalOptions settings = options.Value;

    public async Task SendLeadAssignedAsync(
        int salesExecutiveId,
        int leadId,
        string customerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("OneSignal notification skipped because AppId or ApiKey is not configured.");
            return;
        }

        var payload = new
        {
            app_id = settings.AppId,
            headings = new { en = "New lead assigned" },
            contents = new { en = $"New lead assigned: {customerName}" },
            // Android plays this bundled ten-second tone even when Flutter is not running.
            android_sound = "lead_notification",
            existing_android_channel_id = "lead_assignments_v1",
            include_aliases = new Dictionary<string, string[]>
            {
                ["external_id"] = [$"crm-user-{salesExecutiveId}"]
            },
            target_channel = "push",
            data = new
            {
                screen = "assigned_leads",
                leadId
            }
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
                    "OneSignal rejected lead {LeadId} notification with status {StatusCode}: {ResponseBody}",
                    leadId, (int)response.StatusCode, responseBody);
            }
        }
        catch (Exception exception)
        {
            // Lead assignment must remain successful if the external push service is unavailable.
            logger.LogError(exception, "Could not send OneSignal notification for lead {LeadId}.", leadId);
        }
    }
}
