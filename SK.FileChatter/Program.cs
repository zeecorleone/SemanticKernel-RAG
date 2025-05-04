// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SK.FileChatter.Services;

Console.WriteLine("Starting..\n\n");

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

//Sementic Kernel
services.AddSingleton(sp =>
{
    var builder = Kernel.CreateBuilder();
    var openAiApiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
    //Adding OpenAI Services
    builder.AddOpenAIChatCompletion(
        modelId: configuration["OpenAI:ModelId"] ?? "gpt-4",
        apiKey: openAiApiKey
        );

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    builder.AddOpenAITextEmbeddingGeneration(
        modelId: configuration["OpenAI:EmbeddingModelId"] ?? "text-embedding-ada-002",
        apiKey: openAiApiKey
        );
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


    return builder.Build();
});


//Vector Db
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
services.AddSingleton<ISemanticTextMemory>(sp =>
{
    var endpoint = configuration["Qdrant:Endpoint"];
    var collectionName = configuration["Qdrant:CollectionName"];

    var qdrantClient = new QdrantClient(endpoint);

    int vectorSize = 1536;
    var distance = Distance.Cosine;

    //check if the collection exists:
    var collectionExists = qdrantClient.CollectionExistsAsync(collectionName).GetAwaiter().GetResult();    
    if(!collectionExists)
    {
        qdrantClient.CreateCollectionAsync(
            collectionName: collectionName,
            vectorsConfig: new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = distance
            })
        .GetAwaiter()
        .GetResult();
    }

    var memoryStore = new QdrantMemoryStore(endpoint, vectorSize);

    //var vectorStore = new QdrantVectorStore(qdrantClient);
    var kernel = sp.GetRequiredService<Kernel>();
    var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

    return new MemoryBuilder()
    .WithMemoryStore(memoryStore)
    .WithTextEmbeddingGeneration(embeddingService)
    .Build();
    
    #pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
});

services.AddSingleton<VectorDbService>(sp =>
{
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    var memory = sp.GetRequiredService<ISemanticTextMemory>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    var logger = sp.GetRequiredService<ILogger<VectorDbService>>();
    var collectionName = configuration["Qdrant:CollectionName"] ?? "pdf_documents";
    var maxResults = configuration.GetValue<int>("Application:MaxResultsCount", 5);
    return new VectorDbService(memory, collectionName, maxResults, logger);
});

services.AddSingleton<PdfProcessingService>();
services.AddSingleton<ChatService>();