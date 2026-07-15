namespace backend.Dtos;

public record CreateVehicleBookingRequest(
    DateOnly VisitDate,
    int PersonCount,
    string VisitPlace,
    string PickupPlace,
    DateTime ClientLocalDateTime,
    int TimezoneOffsetMinutes);

public record ReviewVehicleBookingRequest(string? Remarks);
