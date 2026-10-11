namespace TramWebApp.Infrastructure.InMemory;

using System.Collections.Concurrent;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<string, Guid> _byUsername = new();
    private readonly ConcurrentDictionary<string, Guid> _byEmail = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_byUsername.TryGetValue(username, out var id))
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }
        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_byEmail.TryGetValue(email, out var id))
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }
        return Task.FromResult<User?>(null);
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.TryAdd(user.Id, user);
        _byUsername.TryAdd(user.Username, user.Id);
        _byEmail.TryAdd(user.Email, user.Id);
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.AddOrUpdate(user.Id, user, (_, _) => user);
        return Task.FromResult(user);
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_byUsername.ContainsKey(username));
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_byEmail.ContainsKey(email));
    }
}