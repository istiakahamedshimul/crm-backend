namespace backend.Dtos;

public record SaveVehicleRequest(string RegistrationNumber, string VehicleType, string Brand,
    string Model, string? Color, int SeatingCapacity, bool IsActive = true);
