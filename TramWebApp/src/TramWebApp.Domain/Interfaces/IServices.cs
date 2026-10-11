namespace TramWebApp.Domain.Interfaces;

public interface IModelService
{
    Task<ModelOutput> PredictAsync(ModelInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelOutput>> PredictBatchAsync(IEnumerable<ModelInput> inputs, CancellationToken cancellationToken = default);
    Task<MetricsSnapshot> CalculateMetricsAsync(ModelInput input, ModelOutput output, double? actualSpeed = null, double? actualPosition = null, CancellationToken cancellationToken = default);
    bool IsModelLoaded { get; }
    string ModelVersion { get; }
}

public interface IAuthService
{
    Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> RegisterAsync(string username, string email, string password, UserRole role = UserRole.User, CancellationToken cancellationToken = default);
    string GenerateJwtToken(User user);
    bool ValidateToken(string token, out Guid userId);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public interface IUnitOfWork : IDisposable
{
    ITramRunRepository TramRuns { get; }
    ITramTelemetryRepository TelemetryPoints { get; }
    IModelInferenceRepository Inferences { get; }
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}