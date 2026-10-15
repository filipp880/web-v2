namespace TramWebApp.Infrastructure.InMemory;
using System.Collections.Concurrent;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
public class InMemoryModelInferenceRepository(ITramRunRepository runs) : IModelInferenceRepository
{
    private readonly ConcurrentDictionary<Guid, ModelInference> _inferences = new();
    private readonly object _gate = new();
    public Task<ModelInference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_inferences.GetValueOrDefault(id));
    public Task<IReadOnlyList<ModelInference>> GetByRunIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ModelInference>>(_inferences.Values.Where(item => item.TramRunId == id).OrderBy(item => item.InferenceTime).ToArray());
    public Task<IReadOnlyList<ModelInference>> GetByUserIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ModelInference>>(_inferences.Values.Where(item => item.UserId == id).OrderByDescending(item => item.InferenceTime).ToArray());
    public async Task<ModelInference> AddAsync(ModelInference inference, CancellationToken cancellationToken = default)
    {
        var run = await runs.GetByIdAsync(inference.TramRunId, cancellationToken) ?? throw new EntityNotFoundException("TramRun", inference.TramRunId);
        lock (_gate) { if (_inferences.TryAdd(inference.Id, inference)) run.Inferences.Add(inference); }
        return inference;
    }
    public Task<ModelInference> UpdateAsync(ModelInference inference, CancellationToken cancellationToken = default)
    {
        _inferences[inference.Id] = inference; return Task.FromResult(inference);
    }
    public Task<int> GetCountByRunIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_inferences.Values.Count(item => item.TramRunId == id));
    public async Task DeleteByRunAsync(Guid id)
    {
        var run = await runs.GetByIdAsync(id);
        lock (_gate)
        {
            foreach (var item in _inferences.Values.Where(item => item.TramRunId == id)) _inferences.TryRemove(item.Id, out _);
            run?.Inferences.Clear();
        }
    }
}
