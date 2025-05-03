
namespace SK.FileChatter.Models;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<TextChunk> SourceChunks { get; set; } = new();
}
