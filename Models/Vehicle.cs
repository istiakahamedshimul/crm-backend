namespace backend.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string? Color { get; set; }
    public int SeatingCapacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
