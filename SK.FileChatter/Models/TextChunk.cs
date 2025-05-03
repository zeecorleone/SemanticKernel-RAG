

namespace SK.FileChatter.Models;

public class TextChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string? Section { get; set; }
    public int Position { get; set; }
}
