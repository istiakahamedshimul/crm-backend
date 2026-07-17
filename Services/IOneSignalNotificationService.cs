namespace backend.Services;

public interface IOneSignalNotificationService
{
    Task SendLeadAssignedAsync(int salesExecutiveId, int leadId, string customerName, CancellationToken cancellationToken = default);
}
