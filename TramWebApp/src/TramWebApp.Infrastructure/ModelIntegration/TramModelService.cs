namespace TramWebApp.Infrastructure.ModelIntegration;

using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.ValueObjects;

public class TramModelService : IModelService
{
    private readonly double[] _modelWeights;
    private readonly double _wheelScaleFactor;
    private bool _isLoaded = false;

    public TramModelService()
    {
        _modelWeights = new double[] { 0.45, 0.35, 0.20 };
        _wheelScaleFactor = 1.0;
        _isLoaded = true;
    }

    public bool IsModelLoaded => _isLoaded;
    public string ModelVersion => "1.0.0-mock";

    public Task<ModelOutput> PredictAsync(ModelInput input, CancellationToken cancellationToken = default)
    {
        if (!_isLoaded)
            throw new InvalidOperationException("Модель не загружена");

        var predictedSpeed = CalculateSpeed(input);
        var predictedPosition = CalculatePosition(input, predictedSpeed);

        return Task.FromResult(new ModelOutput(predictedSpeed, predictedPosition, input.Timestamp, 0.95));
    }

    public Task<IReadOnlyList<ModelOutput>> PredictBatchAsync(IEnumerable<ModelInput> inputs, CancellationToken cancellationToken = default)
    {
        var results = new List<ModelOutput>();
        foreach (var input in inputs)
        {
            results.Add(PredictAsync(input, cancellationToken).Result);
        }
        return Task.FromResult<IReadOnlyList<ModelOutput>>(results);
    }

    public Task<MetricsSnapshot> CalculateMetricsAsync(ModelInput input, ModelOutput output, double? actualSpeed = null, double? actualPosition = null, CancellationToken cancellationToken = default)
    {
        var speedError = actualSpeed.HasValue ? Math.Abs(output.PredictedSpeed - actualSpeed.Value) : 0;
        var positionError = actualPosition.HasValue ? Math.Abs(output.PredictedPosition - actualPosition.Value) : 0;

        var snapshot = new MetricsSnapshot
        {
            CurrentSpeed = output.PredictedSpeed,
            CurrentPosition = output.PredictedPosition,
            PredictedSpeed = output.PredictedSpeed,
            PredictedPosition = output.PredictedPosition,
            ActualSpeed = actualSpeed ?? 0,
            ActualPosition = actualPosition ?? 0,
            SpeedError = speedError,
            PositionError = positionError,
            ControllerPosition = input.ControllerHandlePosition,
            FrontBogieSpeed = input.FrontBogieSpeed,
            RearBogieSpeed = input.RearBogieSpeed,
            IsUserIntervention = false,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = 0
        };

        return Task.FromResult(snapshot);
    }

    private double CalculateSpeed(ModelInput input)
    {
        var avgBogieSpeed = (input.FrontBogieSpeed + input.RearBogieSpeed) / 2.0;
        var controllerFactor = Math.Max(0, input.ControllerHandlePosition);
        
        var speed = avgBogieSpeed * _wheelScaleFactor * (0.8 + 0.4 * controllerFactor);
        
        if (input.ControllerHandlePosition < 0)
        {
            speed *= Math.Max(0.1, 1.0 + input.ControllerHandlePosition * 0.5);
        }

        return Math.Max(0, speed);
    }

    private double CalculatePosition(ModelInput input, double predictedSpeed)
    {
        return predictedSpeed * input.Timestamp * _wheelScaleFactor;
    }

    public void SetWheelScaleFactor(double factor)
    {
        _wheelScaleFactor = Math.Max(0.1, Math.Min(2.0, factor));
    }

    public double GetWheelScaleFactor() => _wheelScaleFactor;
}