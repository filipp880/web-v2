namespace TramWebApp.Application.DTOs;

public record TramTelemetryPointDto(
    Guid Id,
    Guid TramRunId,
    int SequenceNumber,
    double Timestamp,
    double ControllerHandlePosition,
    double FrontBogieSpeed,
    double RearBogieSpeed,
    double? GpsLatitude,
    double? GpsLongitude,
    double? GpsSpeed
)
{
    public double? DistanceMeters { get; init; }
}

public record CreateTelemetryPointDto(
    int SequenceNumber,
    double Timestamp,
    double ControllerHandlePosition,
    double FrontBogieSpeed,
    double RearBogieSpeed,
    double? GpsLatitude = null,
    double? GpsLongitude = null,
    double? GpsSpeed = null
);
