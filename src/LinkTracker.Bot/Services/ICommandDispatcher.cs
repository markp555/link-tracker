namespace LinkTracker.Bot.Services;

public interface ICommandDispatcher
{
    /// <summary>
    /// Processes message sent by user
    /// </summary>
    /// <param name="bot">Telegram bot</param>
    /// <param name="message">Message</param>
    /// <param name="repository">Repository wiyth user data</param>
    /// <returns>True, if message was proceeded, false otherwise</returns>
    public Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, Repository.IUsersRepository repository);

    /// <summary>
    /// Cancels current dialog with user (for example: when user enters another command)
    /// </summary>
    public Task Cancel();
}