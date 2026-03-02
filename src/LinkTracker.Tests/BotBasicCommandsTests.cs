using LinkTracker.Bot.Services;
using LinkTracker.UnitTests;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework.Internal;

namespace LinkTracker.Tests;

[TestFixture]
public class BotBasicCommandsTests
{
    TelegramBotTestFactory botTestFactory;
    HttpClient client;
    TelegramBotMock botMock;

    [OneTimeSetUp]
    public void SetupHost()
    {
        botTestFactory = new();
        client = botTestFactory.CreateDefaultClient();
        botMock = botTestFactory.TelegramClientMock;
    }

    [OneTimeTearDown]
    public void Shutdown()
    {
        client.Dispose();
        botTestFactory.Dispose();
    }

    [SetUp]
    public void Setup()
    {

    }

    /// <summary>
    /// Sends given message and checks bot answer
    /// </summary>
    /// <param name="send">message to send</param>
    /// <returns>recieved answer from bot</returns>
    [TestCase("/start", ExpectedResult = "Welcome to bot!")]
    [TestCase("/unknown", ExpectedResult = "Sorry, I can't process your message: \"/unknown\"\nIf you think this is an error, contact administarators of this bot")]
    [TestCase("lol", ExpectedResult = "Sorry, I can't process your message: \"lol\"\nIf you think this is an error, contact administarators of this bot")]
    [TestCase("/help", ExpectedResult = "Avaiable commands:\n/start - Begin communication with bot\n/help - Shows information about avaiable commands\n")]
    public async Task<string> TestMessage(string send)
    {
        using var dialog = botMock.StartDialog(123);
        await dialog.SendMessage(send);
        string res = dialog.RecieveMessage();
        Assert.That(dialog.HasRecievedMessages, Is.False);
        return res.Replace("\r\n", "\n");
    }
}
