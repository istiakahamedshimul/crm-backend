namespace backend.Dtos;

public record LocationPointRequest(
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    double? SpeedMetersPerSecond,
    double? Heading,
    double? AltitudeMeters,
    bool IsMocked,
    DateTime RecordedAtUtc);

public record LocationBatchRequest(List<LocationPointRequest> Points);
