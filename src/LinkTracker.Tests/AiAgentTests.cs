using LinkTracker.AIAgentService;
using LinkTracker.Dto;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.Tests;
public class AiAgentTests
{
    public static IOptions<AIAgentSettings> AiOptions => Options.Create(new AIAgentSettings(
        new FilteringSettings(["spam", "ads"], ["bot1", "bot2"], 15),
        new SummarizationSetting(60)
        ));

    public static IMessageProcessor Processor => new AIProcessor(AiOptions, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AIProcessor>());

    public static LinkUpdateDto MakeUpdate(string text)
    {
        return new LinkUpdateDto
        {
            Description = text,
            TgChatIds = [1],
            Id = 1,
            Url = "http://google.com/"
        };
    }

    public static string LongText => " This article is about software programming principles. For the fundamental state of matter, see Solid. For other uses, see Solid (disambiguation).\r\nSOLID\r\nPrinciples\r\nSingle responsibility\r\nOpen–closed\r\nLiskov substitution\r\nInterface segregation\r\nDependency inversion\r\nvte\r\nIn object-oriented programming, SOLID is a mnemonic acronym for five principles intended to make source code more understandable, flexible, and maintainable. Although the principles apply to object-oriented programming, they can also form a core philosophy for methodologies such as agile software development and adaptive software development.[1]\r\n\r\nSoftware engineer and instructor Robert C. Martin[2][3][1] introduced the basic principles of SOLID design in his 2000 paper Design Principles and Design Patterns about software rot.[3][4]: 2–3  The SOLID acronym was coined around 2004 by Michael Feathers.[5]";

    public static LinkUpdateDto MakeLongUpdate(string text) => MakeUpdate(text + LongText);

    [Test]
    public async Task Filtered_ByAuthor_Test()
    {
        Assert.That(await Processor.Filter(MakeLongUpdate("user bot2: hello")), Is.False);
    }

    [Test]
    public async Task Filtered_ByStopWords_Test()
    {
        Assert.That(await Processor.Filter(MakeLongUpdate("user x: spam spam spam")), Is.False);
    }

    [Test]
    public async Task Filtered_ByLength_Test()
    {
        Assert.That(await Processor.Filter(MakeUpdate("user x: ok")), Is.False);
    }

    [Test]
    public async Task NotFiltered_Test()
    {
        Assert.That(await Processor.Filter(MakeLongUpdate("user x: this is solution of your problem:")), Is.True);
    }

    [Test]
    public async Task Process_NotShorten_Test()
    {
        var upd = MakeUpdate("user x: hi");
        Assert.That(await Processor.Process(upd), Is.EqualTo(upd));
    }

    [Test]
    public async Task Process_Shorten_Test()
    {
        var res = await Processor.Process(MakeLongUpdate("user x: solution: "));
        Assert.That(res.Description?.Length, Is.Not.GreaterThan(60));
    }
}
