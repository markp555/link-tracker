using LinkTracker.Bot.Repository;
using System.Text;

namespace LinkTracker.Bot.Services;

public class MessageDispatcher
{
    public ICommandDispatcher? DefaultDispatcher { get; set; } = null;
    private Dictionary<string, ICommandDispatcherFactory> _commands = [];
    private ILogger<MessageDispatcher> _logger;
    private IUsersRepository _repository;
    private IWebHostEnvironment hostEnvironment;
    public ITelegramBot Bot { get; }

    public MessageDispatcher(ITelegramBot bot, ILogger<MessageDispatcher> logger, IUsersRepository repository, IWebHostEnvironment webHostEnvironment)
    {
        bot.OnIncomingMessage += (s, e) =>
        {
            return ProcessMessage(e);
        };
        _logger = logger;
        _repository = repository;
        Bot = bot;
        hostEnvironment = webHostEnvironment;
    }

    private Task<bool> TryProcessMessage(IncomingMessage message, ICommandDispatcher? dispatcher)
    {
        if (dispatcher == null)
        {
            return Task.FromResult(false);
        }
        return dispatcher.ProcessMessage(Bot, message, _repository);
    }

    /// <summary>
    /// If app is in development mode, adding error text to message
    /// </summary>
    private string FormatError(string message, string details)
    {
        if (hostEnvironment.IsDevelopment())
        {
            return message + "\n" + details;
        }
        return message;
    }

    protected async Task ProcessMessage(IncomingMessage message)
    {
        try
        {
            ICommandDispatcher? currentDispatcher = (await _repository.LoadUserState(message.From)) as ICommandDispatcher;
            if (_commands.TryGetValue(message.Content, out ICommandDispatcherFactory? factory))
            {
                _logger.LogInformation("Processed command \"{@MessageContent}\"", message.Content);
                await (currentDispatcher?.Cancel() ?? Task.CompletedTask);
                await _repository.SaveUserState(message.From, currentDispatcher = factory.CreateCommandDispatcher(this, message));
            }
            try
            {
                bool ok = await TryProcessMessage(message, currentDispatcher);
                if (!ok)
                {
                    _logger.LogInformation("Current dispatcher for user {UserId} failed to process message: \"{@MessageContent}\"", message.From, message.Content);
                    ok = await TryProcessMessage(message, DefaultDispatcher);
                    if (!ok)
                    {
                        _logger.LogInformation("Default dispatcher failed to process message: \"{@MessageContent}\"", message.Content);
                        await Bot.SendMessageAsync(new OutgoingMessage(message.From, $"Sorry, I can't process your message: \"{message.Content}\"\nIf you think this is an error, contact administarators of this bot"));
                    }
                }
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Scrapper - Bot connection error");
                await _repository.SaveUserState(message.From, null);
                await Bot.SendMessageAsync(new(message.From, FormatError("Sorry, there was an unexpected error processing your message", ex.ToString())));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during processing message");
                await _repository.SaveUserState(message.From, null);
                await Bot.SendMessageAsync(new(message.From, FormatError("There was some problems on our side. Please try again or contact administrators of this bot", ex.Message)));
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing message failed: {@Message}", message);
        }
    }

    public void RegisterCommand(string commandName, ICommandDispatcherFactory commandDispatcherFactory)
    {
        bool ok = _commands.TryAdd(commandName, commandDispatcherFactory);
        if (!ok)
        {
            throw new InvalidOperationException("Bot command already registered");
        }
        _logger.LogInformation("Registered command {command}", commandName);
    }

    public void UnregisterCommand(string commandName)
    {
        bool ok = _commands.Remove(commandName);
        if (!ok)
        {
            throw new InvalidOperationException("Bot command with given name not exist!");
        }
        _logger.LogInformation("Unregistered command {command}", commandName);
    }

    /// <summary>
    /// Sets command list for telegram bot. Should be called when all commands are registered using RegisterCommand
    /// </summary>
    public async Task SendCommandListAsync()
    {
        await Bot.RegisterCommands(_commands.Select(x => new BotCommand(x.Key, x.Value.Description)));
        _logger.LogInformation("Sended commands list to Telegram");
    }

    public string GetHelpText()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("Avaiable commands:");
        foreach (var item in _commands)
        {
            stringBuilder.AppendLine($"{item.Key} - {item.Value.Description}");
        }
        return stringBuilder.ToString();
    }
}