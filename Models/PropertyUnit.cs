namespace backend.Models;

public class PropertyUnit
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string? TowerOrBlock { get; set; }
    public int? FloorNumber { get; set; }
    public string UnitNumber { get; set; } = "";
    public decimal SizeSqft { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public string? FacingDirection { get; set; }
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal BookingMoney { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;
}
