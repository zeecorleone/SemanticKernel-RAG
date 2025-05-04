
using Microsoft.SemanticKernel;
using SK.FileChatter.Services;
using System.ComponentModel;

namespace SK.FileChatter.Plugins;

public class DocumentPlugin
{
    private readonly PdfProcessingService _pdfService;
    private readonly VectorDbService _vectorDbService;

    public DocumentPlugin(VectorDbService vectorDbService, PdfProcessingService pdfService)
    {
        _vectorDbService = vectorDbService;
        _pdfService = pdfService;
    }

    [KernelFunction]
    [Description("Processes a PDF file and stores its contents in the vector database")]
    public async Task<string> ProcessPdfFileAsync([Description("The path to the PDF file")] string filePath)
    {
        if (!File.Exists(filePath))
        {
            return $"File not found: {filePath}";
        }

        try
        {
            var chunks = await _pdfService.ProcessPdfAsync(filePath);
            await _vectorDbService.StoreChunksAsync(chunks);
            return $"Successfully processed {chunks.Count} chunks from file: {filePath}, and stored its embeddings in vector db";
        }
        catch(Exception ex)
        {
            return $"Error processing PDF: {ex.Message}";
        }
    }
}
