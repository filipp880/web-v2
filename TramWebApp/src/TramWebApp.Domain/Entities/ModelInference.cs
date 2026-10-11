namespace TramWebApp.Domain.Entities;

public class ModelInference
{
    public Guid Id { get; private set; }
    public Guid TramRunId { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime InferenceTime { get; private set; }
    public double InputControllerPosition { get; private set; }
    public double InputFrontSpeed { get; private set; }
    public double InputRearSpeed { get; private set; }
    public double PredictedSpeed { get; private set; }
    public double PredictedPosition { get; private set; }
    public double? ActualSpeed { get; private set; }
    public double? ActualPosition { get; private set; }
    public double? UserAdjustedSpeed { get; private set; }
    public double? UserAdjustedPosition { get; private set; }
    public bool IsUserIntervention { get; private set; }
    public double? ErrorSpeed { get; private set; }
    public double? ErrorPosition { get; private set; }
    
    public TramRun TramRun { get; private set; } = null!;

    private ModelInference() { }

    public ModelInference(
        Guid tramRunId,
        Guid? userId,
        double inputControllerPosition,
        double inputFrontSpeed,
        double inputRearSpeed,
        double predictedSpeed,
        double predictedPosition,
        double? actualSpeed = null,
        double? actualPosition = null,
        bool isUserIntervention = false,
        double? userAdjustedSpeed = null,
        double? userAdjustedPosition = null)
    {
        Id = Guid.NewGuid();
        TramRunId = tramRunId;
        UserId = userId;
        InferenceTime = DateTime.UtcNow;
        InputControllerPosition = inputControllerPosition;
        InputFrontSpeed = inputFrontSpeed;
        InputRearSpeed = inputRearSpeed;
        PredictedSpeed = predictedSpeed;
        PredictedPosition = predictedPosition;
        ActualSpeed = actualSpeed;
        ActualPosition = actualPosition;
        IsUserIntervention = isUserIntervention;
        UserAdjustedSpeed = userAdjustedSpeed;
        UserAdjustedPosition = userAdjustedPosition;
        
        if (actualSpeed.HasValue)
            ErrorSpeed = Math.Abs(predictedSpeed - actualSpeed.Value);
        if (actualPosition.HasValue)
            ErrorPosition = Math.Abs(predictedPosition - actualPosition.Value);
    }

    public void SetUserIntervention(double adjustedSpeed, double adjustedPosition)
    {
        IsUserIntervention = true;
        UserAdjustedSpeed = adjustedSpeed;
        UserAdjustedPosition = adjustedPosition;
        
        if (ActualSpeed.HasValue)
            ErrorSpeed = Math.Abs(adjustedSpeed - ActualSpeed.Value);
        if (ActualPosition.HasValue)
            ErrorPosition = Math.Abs(adjustedPosition - ActualPosition.Value);
    }

    public void SetActualValues(double actualSpeed, double actualPosition)
    {
        ActualSpeed = actualSpeed;
        ActualPosition = actualPosition;
        
        ErrorSpeed = Math.Abs(PredictedSpeed - actualSpeed);
        ErrorPosition = Math.Abs(PredictedPosition - actualPosition);
        
        if (IsUserIntervention && UserAdjustedSpeed.HasValue)
        {
            ErrorSpeed = Math.Abs(UserAdjustedSpeed.Value - actualSpeed);
            ErrorPosition = Math.Abs(UserAdjustedPosition.Value - actualPosition);
        }
    }
}