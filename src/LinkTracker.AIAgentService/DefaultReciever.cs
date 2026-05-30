using LinkTracker.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;
internal class DefaultReciever(IMessageProcessor processor, IMessageSender sender, ILogger<DefaultReciever> logger) : IMessageReciever
{
    public async ValueTask HandleMessage(LinkUpdateDto message)
    {
        logger.LogInformation("Processing update {@Update}", message);
        var processed = await processor.Process(message);
        logger.LogDebug("""
            Summarized "{old}" -> "{new}"
            """, message.Description, processed.Description);
        await sender.SendMessageToBot(processed);
    }
}
