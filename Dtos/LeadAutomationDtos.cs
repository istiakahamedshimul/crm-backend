namespace backend.Dtos;

public record LeadAutomationSettingsDto(decimal UnassignAfterHours, decimal ReminderIntervalHours);
public record UpdateLeadAutomationSettingsRequest(decimal UnassignAfterHours, decimal ReminderIntervalHours);
