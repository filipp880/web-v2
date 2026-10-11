namespace TramWebApp.Domain.ValueObjects;

public readonly record struct ModelOutput
{
    public double PredictedSpeed { get; init; }
    public double PredictedPosition { get; init; }
    public double Timestamp { get; init; }
    public double Confidence { get; init; }

    public ModelOutput(double predictedSpeed, double predictedPosition, double timestamp, double confidence = 1.0)
    {
        PredictedSpeed = predictedSpeed;
        PredictedPosition = predictedPosition;
        Timestamp = timestamp;
        Confidence = confidence;
    }
}