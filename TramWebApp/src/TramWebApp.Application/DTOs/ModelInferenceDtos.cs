namespace TramWebApp.Application.DTOs;

public record ModelInferenceDto(
    Guid Id,
    Guid TramRunId,
    Guid? UserId,
    DateTime InferenceTime,
    double InputControllerPosition,
    double InputFrontSpeed,
    double InputRearSpeed,
    double PredictedSpeed,
    double PredictedPosition,
    double? ActualSpeed,
    double? ActualPosition,
    double? UserAdjustedSpeed,
    double? UserAdjustedPosition,
    bool IsUserIntervention,
    double? ErrorSpeed,
    double? ErrorPosition
);

public record ModelInferenceListDto(
    Guid Id,
    Guid TramRunId,
    DateTime InferenceTime,
    double PredictedSpeed,
    double PredictedPosition,
    bool IsUserIntervention,
    double? ErrorSpeed,
    double? ErrorPosition
);

public record ModelInputDto(
    double ControllerHandlePosition,
    double FrontBogieSpeed,
    double RearBogieSpeed,
    double Timestamp,
    double? GpsLatitude = null,
    double? GpsLongitude = null,
    double? GpsSpeed = null
);

public record ModelOutputDto(
    double PredictedSpeed,
    double PredictedPosition,
    double Timestamp,
    double Confidence
);

public record MetricsSnapshotDto(
    double CurrentSpeed,
    double CurrentPosition,
    double PredictedSpeed,
    double PredictedPosition,
    double ActualSpeed,
    double ActualPosition,
    double SpeedError,
    double PositionError,
    double ControllerPosition,
    double FrontBogieSpeed,
    double RearBogieSpeed,
    bool IsUserIntervention,
    DateTime Timestamp,
    int SequenceNumber
);

public record InferenceRequestDto(
    Guid TramRunId,
    int SequenceNumber,
    ModelInputDto Input,
    double? UserAdjustedSpeed = null,
    double? UserAdjustedPosition = null
);