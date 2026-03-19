using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using PeDeYaf.Application.Common.Interfaces;

namespace PeDeYaf.Infrastructure.Ai;

public class ClaudeAiService(
    AnthropicClient client,
    ILogger<ClaudeAiService> logger) : IAiService
{
    public async Task<List<string>> GenerateTagsAsync(
        string fileName, string textSample, CancellationToken ct = default)
    {
        try
        {
            var messages = new List<Message>
            {
                new()
                {
                    Role = RoleType.User,
                    Content = $"""
                        Analyze this document and generate 3-8 concise, lowercase tags.
                        
                        File name: {fileName}
                        Text sample: {textSample}
                        
                        Return ONLY a JSON array of strings. No explanation. Example: ["invoice","finance","2024"]
                        """,
                },
            };

            var response = await client.Messages.GetClaudeMessageAsync(
                new MessageParameters
                {
                    Messages = messages,
                    MaxTokens = 150,
                    Model = AnthropicModels.Claude35Sonnet,
                    Temperature = 0.2m,
                }, ct);

            var json = response.Content.First().Text;
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI tagging failed for {File}", fileName);
            return [];
        }
    }

    public async Task<string> SummarizePdfAsync(
        string extractedText, int maxWords, CancellationToken ct = default)
    {
        var truncated = extractedText.Length > 150_000
            ? extractedText[..150_000] + "\n[Document truncated...]"
            : extractedText;

        var messages = new List<Message>
        {
            new()
            {
                Role = RoleType.User,
                Content = $"Summarize this document in {maxWords} words or less:\n\n{truncated}",
            },
        };

        var response = await client.Messages.GetClaudeMessageAsync(
            new MessageParameters
            {
                Messages = messages,
                MaxTokens = maxWords * 2,
                Model = AnthropicModels.Claude3Haiku,
            }, ct);

        return response.Content.First().Text;
    }
}
