namespace backend.Models;

public class EmployeeLocation
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AccuracyMeters { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public double? Heading { get; set; }
    public double? AltitudeMeters { get; set; }
    public bool IsMocked { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
}
