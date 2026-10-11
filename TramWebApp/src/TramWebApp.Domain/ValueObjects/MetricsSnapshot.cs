namespace TramWebApp.Domain.ValueObjects;

public readonly record struct MetricsSnapshot
{
    public double CurrentSpeed { get; init; }
    public double CurrentPosition { get; init; }
    public double PredictedSpeed { get; init; }
    public double PredictedPosition { get; init; }
    public double ActualSpeed { get; init; }
    public double ActualPosition { get; init; }
    public double SpeedError { get; init; }
    public double PositionError { get; init; }
    public double ControllerPosition { get; init; }
    public double FrontBogieSpeed { get; init; }
    public double RearBogieSpeed { get; init; }
    public bool IsUserIntervention { get; init; }
    public DateTime Timestamp { get; init; }
    public int SequenceNumber { get; init; }
}