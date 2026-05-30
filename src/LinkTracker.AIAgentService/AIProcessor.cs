using LinkTracker.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;
public class AIProcessor : IMessageProcessor
{
    public ValueTask<LinkUpdateDto> Process(LinkUpdateDto message)
    {
        return ValueTask.FromResult(message);
    }
}
