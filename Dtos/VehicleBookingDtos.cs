namespace backend.Dtos;

public record CreateVehicleBookingRequest(
    int CustomerId,
    int ProjectId,
    DateOnly VisitDate,
    TimeOnly VisitTime,
    int PersonCount,
    string PickupPlace,
    string Purpose,
    string? AdditionalInformation,
    DateTime ClientLocalDateTime,
    int TimezoneOffsetMinutes);

public record ReviewVehicleBookingRequest(int? VehicleId, string? Driver, string? Remarks);

public record CreateAdminVehicleBookingRequest(int CustomerId, int ProjectId, DateOnly VisitDate,
    TimeOnly VisitTime, int PersonCount, string PickupPlace, string Purpose,
    string? AdditionalInformation, int VehicleId, string? Driver, string? Remarks);
