using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;
public interface IMessageProcessor
{
    ValueTask<Dto.LinkUpdateDto> Process(Dto.LinkUpdateDto message);
}
