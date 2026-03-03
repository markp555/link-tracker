using LinkTracker.Bot;
using LinkTracker.Bot.Repository;
using LinkTracker.Bot.Services;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;

namespace LinkTracker.UnitTests;

public class MessageDispatcherTests
{
    private MessageDispatcher dispatcher;
    private ICommandDispatcher commandDispatcher1;
    private ICommandDispatcher commandDispatcher2;
    private ICommandDispatcherFactory factory1;
    private ICommandDispatcherFactory factory2;
    private TelegramBotMock telegramBot;
    private DefaultRepository repository;

    [SetUp]
    public void Setup()
    {
        repository = new DefaultRepository();
        telegramBot = new TelegramBotMock();
        factory1 = Substitute.For<ICommandDispatcherFactory>();
        factory2 = Substitute.For<ICommandDispatcherFactory>();
        commandDispatcher1 = Substitute.For<ICommandDispatcher>();
        commandDispatcher2 = Substitute.For<ICommandDispatcher>();
        factory1.Description.Returns("Lol");
        factory2.Description.Returns("Wow");
        IWebHostEnvironment env = Substitute.For<IWebHostEnvironment>();
        env.ApplicationName.Returns("Bot");
        env.EnvironmentName.Returns("Development");
        dispatcher = new MessageDispatcher(telegramBot, new Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageDispatcher>(), repository, env);
    }

    [Test]
    public async Task Dispatch_OneFactory_Test()
    {
        factory1.ClearReceivedCalls();
        factory1.CreateCommandDispatcher(dispatcher, Arg.Any<IncomingMessage>()).Returns(commandDispatcher1);
        commandDispatcher1.ClearReceivedCalls();
        IncomingMessage msg = new(1, "hi");
        commandDispatcher1.ProcessMessage(telegramBot, msg, repository).Returns(Task.FromResult(true));
        dispatcher.RegisterCommand("hi", factory1);
        await telegramBot.SendUserMessage(msg);
        _ = commandDispatcher1.Received().ProcessMessage(telegramBot, msg, repository);
        factory1.Received().CreateCommandDispatcher(dispatcher, msg);
        dispatcher.UnregisterCommand("hi");
    }

    [Test]
    public async Task Dispatch_OneFactory_MultipleMessages_Test()
    {
        factory1.ClearReceivedCalls();
        factory1.CreateCommandDispatcher(dispatcher, Arg.Any<IncomingMessage>()).Returns(commandDispatcher1);
        commandDispatcher1.ClearReceivedCalls();
        IncomingMessage msg = new(1, "hi"), msg2 = new(1, "lol"), msg3 = new(2, "hi");
        commandDispatcher1.ProcessMessage(telegramBot, msg, repository).Returns(Task.FromResult(true));
        commandDispatcher1.ProcessMessage(telegramBot, msg2, repository).Returns(Task.FromResult(true));
        commandDispatcher1.ProcessMessage(telegramBot, msg3, repository).Returns(Task.FromResult(true));
        dispatcher.RegisterCommand("hi", factory1);
        await telegramBot.SendUserMessage(msg);
        await telegramBot.SendUserMessage(msg2);
        await telegramBot.SendUserMessage(msg3);
        _ = commandDispatcher1.Received().ProcessMessage(telegramBot, msg, repository);
        _ = commandDispatcher1.Received().ProcessMessage(telegramBot, msg2, repository);
        _ = commandDispatcher1.Received().ProcessMessage(telegramBot, msg3, repository);
        factory1.Received().CreateCommandDispatcher(dispatcher, msg);
        factory1.Received().CreateCommandDispatcher(dispatcher, msg3);
        dispatcher.UnregisterCommand("hi");
    }

    [Test]
    public async Task Dispatch_ManyFactories_Test()
    {
        factory1.ClearReceivedCalls();
        factory1.CreateCommandDispatcher(dispatcher, Arg.Any<IncomingMessage>()).Returns(commandDispatcher1);
        factory2.ClearReceivedCalls();
        factory2.CreateCommandDispatcher(dispatcher, Arg.Any<IncomingMessage>()).Returns(commandDispatcher2);
        commandDispatcher1.ClearReceivedCalls();
        commandDispatcher2.ClearReceivedCalls();
        IncomingMessage msg = new(1, "hi"), msg2 = new(1, "lol"), msg3 = new(1, "oh");
        commandDispatcher1.ProcessMessage(telegramBot, msg, repository).Returns(Task.FromResult(true));
        commandDispatcher2.ProcessMessage(telegramBot, msg2, repository).Returns(Task.FromResult(true));
        commandDispatcher2.ProcessMessage(telegramBot, msg3, repository).Returns(Task.FromResult(true));
        dispatcher.RegisterCommand("hi", factory1);
        dispatcher.RegisterCommand("lol", factory2);
        await telegramBot.SendUserMessage(msg);
        _ = commandDispatcher1.Received().ProcessMessage(telegramBot, msg, repository);
        await telegramBot.SendUserMessage(msg2);
        _ = commandDispatcher2.Received().ProcessMessage(telegramBot, msg2, repository);
        await telegramBot.SendUserMessage(msg3);
        _ = commandDispatcher2.Received().ProcessMessage(telegramBot, msg3, repository);
        factory1.Received().CreateCommandDispatcher(dispatcher, msg);
        factory2.Received().CreateCommandDispatcher(dispatcher, msg2);
        dispatcher.UnregisterCommand("hi");
        dispatcher.UnregisterCommand("lol");
    }
}
