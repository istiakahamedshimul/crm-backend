namespace backend.Options;

public sealed class BackupOptions
{
    public string Directory { get; set; } = "/var/backups/crm";
    public string MySqlDumpExecutable { get; set; } = "mysqldump";
    public string[] ProjectPaths { get; set; } = ["/opt/crm/backend-current"];
    public int InstantRetentionHours { get; set; } = 24;
    public int WeeklyIntervalDays { get; set; } = 7;
}
