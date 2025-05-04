
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SK.FileChatter.Models;

namespace SK.FileChatter.Services;

public class ChatService
{
    private readonly Kernel _kernel;
    private readonly VectorDbService _vectorDbService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(Kernel kernel, VectorDbService vectorDbService, ILogger<ChatService> logger)
    {
        _kernel = kernel;
        _vectorDbService = vectorDbService;
        _logger = logger;
    }

    public async Task<Models.ChatResponse> GetResponseAsync(string question)
    {
        _logger.LogInformation("Processing Question: {question}", question);

        var searchResults = await _vectorDbService.SearchAsync(question);

        if(!searchResults.Any())
        {
            return new Models.ChatResponse
            {
                Answer = "No relevant information found."
            };
        }

        //prepare context for search result
        var context = string.Join("\n\n", searchResults.Select(x => x.Metadata.Text));

        //creating a semantic function for answering the question
        var promptTemplate = @"

You are an assistant that answers question based on the provided context from a PDF document.

Context information from the docuemnt:
{{$context}}

User's question: {{$question}}

Instructions:
1. Answer the question using ONLY the information provided in the context above.
2. If the answer cannot be determined from the context, say 'I don't have enough information in the document to answer this question.'
3. Be concise and to the point.
4. Cite the relevant page numbers if that information is available in the context.

Answer:";


        var arguments = new KernelArguments
        {
            { "context", context },
            { "question", question }
        };

        //create prompt function

        var promptFunction = _kernel.CreateFunctionFromPrompt(promptTemplate,
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1000,
                Temperature = 0.0,
                //ModelId = ""
            });

        var result = await promptFunction.InvokeAsync(_kernel, arguments);
        var answer = result.GetValue<string>() ?? "Failed to generate response.";

        //create source chunks from search results
        var sourceChunks = searchResults
            .Select(x => new TextChunk
            {
                Id = x.Metadata.Id,
                Text = x.Metadata.Text,
                PageNumber = ExtractPageNumber(x.Metadata.Description),

            }).ToList();

        return new ChatResponse
        {
            Answer = answer,
            SourceChunks = sourceChunks
        };

    }

    private int ExtractPageNumber(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return 0;

        if (description.StartsWith("Page ") && int.TryParse(description.Substring(5), out int pageNumber))
            return pageNumber;

        return 0;
    }

}
