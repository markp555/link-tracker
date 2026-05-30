using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;
public interface IMessageProcessor
{
    // Returns true if message should be processed, false otherwise
    ValueTask<bool> Filter(Dto.LinkUpdateDto message);
    ValueTask<Dto.LinkUpdateDto> Process(Dto.LinkUpdateDto message);
}
