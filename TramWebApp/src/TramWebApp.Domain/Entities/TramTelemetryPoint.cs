namespace TramWebApp.Domain.Entities;

public class TramTelemetryPoint
{
    public Guid Id { get; private set; }
    public Guid TramRunId { get; private set; }
    public int SequenceNumber { get; private set; }
    public double Timestamp { get; private set; }
    public double ControllerHandlePosition { get; private set; }
    public double FrontBogieSpeed { get; private set; }
    public double RearBogieSpeed { get; private set; }
    public double? GpsLatitude { get; private set; }
    public double? GpsLongitude { get; private set; }
    public double? GpsSpeed { get; private set; }
    public double? DistanceMeters { get; private set; }
    public void SetDistance(double distance) => DistanceMeters = distance;
    
    public TramRun TramRun { get; private set; } = null!;

    private TramTelemetryPoint() { }
    public void UpdateGps(double? latitude, double? longitude, double? speed)
    {
        GpsLatitude = latitude;
        GpsLongitude = longitude;
        GpsSpeed = speed;
    }

    public TramTelemetryPoint(
        Guid tramRunId,
        int sequenceNumber,
        double timestamp,
        double controllerHandlePosition,
        double frontBogieSpeed,
        double rearBogieSpeed,
        double? gpsLatitude = null,
        double? gpsLongitude = null,
        double? gpsSpeed = null)
    {
        Id = Guid.NewGuid();
        TramRunId = tramRunId;
        SequenceNumber = sequenceNumber;
        Timestamp = timestamp;
        ControllerHandlePosition = controllerHandlePosition;
        FrontBogieSpeed = frontBogieSpeed;
        RearBogieSpeed = rearBogieSpeed;
        GpsLatitude = gpsLatitude;
        GpsLongitude = gpsLongitude;
        GpsSpeed = gpsSpeed;
    }
}
