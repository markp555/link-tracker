using LinkTracker.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;

public record FilteringSettings(List<string> StopWords, List<string> ExcludedAuthors, int MinLength);
public record SummarizationSetting(int Threshold);
public record AIAgentSettings(FilteringSettings Filtering, SummarizationSetting Summarization);

public class AIProcessor(IOptions<AIAgentSettings> options, ILogger<AIProcessor> logger) : IMessageProcessor
{
    public ValueTask<bool> Filter(LinkUpdateDto message)
    {
        if (message.Description?.Length < options.Value.Summarization.Threshold)
        {
            return ValueTask.FromResult(false);
        }
        IEnumerable<string> badWords = options.Value.Filtering.ExcludedAuthors.Select(s => "user " + s).Concat(options.Value.Filtering.StopWords);
        if (badWords.Any(s => message.Description?.Contains(s) == true))
        {
            return ValueTask.FromResult(false);
        }
        return ValueTask.FromResult(true);
    }

    public ValueTask<LinkUpdateDto> Process(LinkUpdateDto message)
    {
        string desc = message.Description ?? "";
        if (desc.Length <= options.Value.Summarization.Threshold)
        {
            return ValueTask.FromResult(message);
        }
        return ValueTask.FromResult(new LinkUpdateDto
        {
            TgChatIds = message.TgChatIds,
            Description = desc.Substring(0, options.Value.Summarization.Threshold - 3) + "...",
            Id = message.Id,
            Url = message.Url
        });
    }
}
