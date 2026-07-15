namespace backend.Models;

public class VehicleBooking
{
    public int Id { get; set; }
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public DateOnly VisitDate { get; set; }
    public int PersonCount { get; set; }
    public string VisitPlace { get; set; } = "";
    public string PickupPlace { get; set; } = "";
    public DateTime ClientLocalDateTime { get; set; }
    public int TimezoneOffsetMinutes { get; set; }
    public VehicleBookingStatus Status { get; set; } = VehicleBookingStatus.Pending;
    public string? AdminRemarks { get; set; }
    public int? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
