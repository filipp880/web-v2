namespace TramWebApp.Application.Interfaces;

using TramWebApp.Application.DTOs;

public interface ITramRunUseCases
{
    Task<TramRunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TramRunListDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TramRunListDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TramRunDto> CreateAsync(CreateTramRunDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<TramRunDto> UpdateAsync(Guid id, UpdateTramRunDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

public interface ITelemetryUseCases
{
    Task<IReadOnlyList<TramTelemetryPointDto>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default);
    Task<TramTelemetryPointDto?> GetByRunIdAndSequenceAsync(Guid tramRunId, int sequenceNumber, CancellationToken cancellationToken = default);
    Task<TramTelemetryPointDto> AddAsync(Guid tramRunId, CreateTelemetryPointDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TramTelemetryPointDto>> AddRangeAsync(Guid tramRunId, IEnumerable<CreateTelemetryPointDto> dtos, CancellationToken cancellationToken = default);
}

public interface IModelUseCases
{
    Task<ModelOutputDto> PredictAsync(ModelInputDto input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelOutputDto>> PredictBatchAsync(IEnumerable<ModelInputDto> inputs, CancellationToken cancellationToken = default);
    Task<MetricsSnapshotDto> CalculateMetricsAsync(ModelInputDto input, ModelOutputDto output, double? actualSpeed = null, double? actualPosition = null, CancellationToken cancellationToken = default);
    Task<ModelInferenceDto> RunInferenceAsync(InferenceRequestDto request, Guid? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelInferenceListDto>> GetInferencesByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelInferenceListDto>> GetInferencesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ModelInferenceDto?> GetInferenceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ModelInferenceDto> UpdateInferenceWithUserInterventionAsync(Guid inferenceId, double adjustedSpeed, double adjustedPosition, Guid userId, CancellationToken cancellationToken = default);
    Task<ModelInferenceDto> UpdateInferenceWithActualValuesAsync(Guid inferenceId, double actualSpeed, double actualPosition, CancellationToken cancellationToken = default);
    bool IsModelLoaded { get; }
    string ModelVersion { get; }
}

public interface IAuthUseCases
{
    Task<LoginResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<UserDto?> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}