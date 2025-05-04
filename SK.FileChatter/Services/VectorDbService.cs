
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;

namespace SK.FileChatter.Services;

public class VectorDbService
{
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ISemanticTextMemory _memory;
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly string _collectionName;
    private readonly int _maxResults;
    private readonly ILogger<VectorDbService> _logger;

    public VectorDbService(
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ISemanticTextMemory memory,
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        string collectionName,
        int maxResults,
        ILogger<VectorDbService> logger)
    {
        _memory = memory;
        _collectionName = collectionName;
        _maxResults = maxResults;
        _logger = logger;
    }

    public async Task StoreChunksAsync(List<Models.TextChunk> chunks)
    {
        _logger.LogInformation("Storing {count} chunks in memroy", chunks.Count);

        foreach (var chunk in chunks)
        {
            await _memory.SaveInformationAsync(
                _collectionName,
                chunk.Text,
                chunk.Id,
                description: $"Page {chunk.PageNumber}",
                additionalMetadata: $"Position: {chunk.Position}");
        }
    }

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public async Task<List<MemoryQueryResult>> SearchAsync(string query)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        _logger.LogInformation("Searching for query: {query}", query);

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var resultsList = new List<MemoryQueryResult>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        await foreach (var result in _memory.SearchAsync(_collectionName, query, _maxResults))
        {
            resultsList.Add(result);
        }

        return resultsList;
    }

}