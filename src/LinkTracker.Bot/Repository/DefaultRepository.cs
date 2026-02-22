
namespace LinkTracker.Bot.Repository;

public class DefaultRepository : IUsersRepository
{
    private Dictionary<long, object> _users = [];
    public Task<object?> LoadUserState(long userId)
    {
        _ = _users.TryGetValue(userId, out var state);
        return Task.FromResult(state);
    }

    public Task SaveUserState(long userId, object? data)
    {
        if (data == null)
        {
            _users.Remove(userId);
        }
        else
        {
            _users.Remove(userId);
            _users.Add(userId, data);
        }
        return Task.CompletedTask;
    }
}