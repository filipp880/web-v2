namespace TramWebApp.Domain.Interfaces;

public interface ITramRunRepository
{
    Task<TramRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TramRun>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TramRun>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TramRun> AddAsync(TramRun tramRun, CancellationToken cancellationToken = default);
    Task<TramRun> UpdateAsync(TramRun tramRun, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITramTelemetryRepository
{
    Task<IReadOnlyList<TramTelemetryPoint>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default);
    Task<TramTelemetryPoint?> GetByRunIdAndSequenceAsync(Guid tramRunId, int sequenceNumber, CancellationToken cancellationToken = default);
    Task<TramTelemetryPoint> AddAsync(TramTelemetryPoint point, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TramTelemetryPoint>> AddRangeAsync(IEnumerable<TramTelemetryPoint> points, CancellationToken cancellationToken = default);
    Task<int> GetCountByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default);
}

public interface IModelInferenceRepository
{
    Task<ModelInference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelInference>> GetByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelInference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ModelInference> AddAsync(ModelInference inference, CancellationToken cancellationToken = default);
    Task<ModelInference> UpdateAsync(ModelInference inference, CancellationToken cancellationToken = default);
    Task<int> GetCountByRunIdAsync(Guid tramRunId, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}