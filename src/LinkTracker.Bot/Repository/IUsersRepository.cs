namespace LinkTracker.Bot.Repository;

public interface IUsersRepository
{
    public Task SaveUserState(long userId, object? data);

    public Task<object?> LoadUserState(long userId);
}