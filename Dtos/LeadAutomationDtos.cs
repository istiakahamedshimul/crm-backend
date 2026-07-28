namespace backend.Dtos;

public record LeadAutomationSettingsDto(int UnassignAfterHours, int ReminderIntervalHours);
public record UpdateLeadAutomationSettingsRequest(int UnassignAfterHours, int ReminderIntervalHours);
