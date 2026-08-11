namespace backend.Services;

public interface IOneSignalNotificationService
{
    Task SendLeadAssignedAsync(int salesExecutiveId, int leadId, string customerName, CancellationToken cancellationToken = default);
    Task SendLeadFollowUpReminderAsync(int salesExecutiveId, int leadId, string customerName, CancellationToken cancellationToken = default);
    Task SendFinancialPushAsync(int salesExecutiveId, int customerId, string title, string message, CancellationToken cancellationToken = default);
}
