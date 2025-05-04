
using Microsoft.SemanticKernel;
using SK.FileChatter.Services;
using System.ComponentModel;

namespace SK.FileChatter.Plugins;

public class ChatPlugin
{
    private readonly ChatService _chatService;

    public ChatPlugin(ChatService chatService)
    {
        _chatService = chatService;
    }

    [KernelFunction]
    [Description("Ask a question about a PDF document")]
    public async Task<string> AskQuestionAsync(
        [Description("The question to ask about the document")] string question)
    {
        var response = await _chatService.GetResponseAsync(question);

        var result = $"\n\nAssistant:\n{response.Answer}\n\n";

        if (response.SourceChunks?.Count > 0)
        {
            result += "Source:\n";
            foreach (var chunk in response.SourceChunks)
            {
                //result += $"- {chunk.Text} (Page {chunk.PageNumber})\n";
                result += $"(Page {chunk.PageNumber})\n";
            }
        }
        return result;
    }
}