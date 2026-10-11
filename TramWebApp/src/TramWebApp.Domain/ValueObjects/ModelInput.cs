namespace TramWebApp.Domain.ValueObjects;

public readonly record struct ModelInput
{
    public double ControllerHandlePosition { get; init; }
    public double FrontBogieSpeed { get; init; }
    public double RearBogieSpeed { get; init; }
    public double Timestamp { get; init; }
    public double? GpsLatitude { get; init; }
    public double? GpsLongitude { get; init; }
    public double? GpsSpeed { get; init; }

    public ModelInput(
        double controllerHandlePosition,
        double frontBogieSpeed,
        double rearBogieSpeed,
        double timestamp,
        double? gpsLatitude = null,
        double? gpsLongitude = null,
        double? gpsSpeed = null)
    {
        ControllerHandlePosition = controllerHandlePosition;
        FrontBogieSpeed = frontBogieSpeed;
        RearBogieSpeed = rearBogieSpeed;
        Timestamp = timestamp;
        GpsLatitude = gpsLatitude;
        GpsLongitude = gpsLongitude;
        GpsSpeed = gpsSpeed;
    }
}