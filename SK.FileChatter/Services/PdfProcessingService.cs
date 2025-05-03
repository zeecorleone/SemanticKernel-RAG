
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using SK.FileChatter.Models;

namespace SK.FileChatter.Services;

public class PdfProcessingService
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlapSize;
    private readonly ILogger<PdfProcessingService> _logger;

    public PdfProcessingService(IConfiguration config, ILogger<PdfProcessingService> logger)
    {
        _logger = logger;
        _chunkSize = config.GetValue<int>("Application:ChunkSize");
        _chunkOverlapSize = config.GetValue<int>("Application:ChunkOverlap");
    }

    public async Task<List<Models.TextChunk>> ProcessPdfAsync(string pdfPath)
    {
        _logger.LogInformation("Processing PDF file: {PdfPath}", pdfPath);

        var extractedText = ExtractTextFromPdf(pdfPath);
        var chunks = SplitIntoChunks(extractedText);

        return null;
    }

    private Dictionary<int, string> ExtractTextFromPdf(string pdfPath)
    {
        var result = new Dictionary<int, string>();
        
        using var pdfReader = new PdfReader(pdfPath);
        using var pdfDocument = new PdfDocument(pdfReader);

        int numberOfPages = pdfDocument.GetNumberOfPages();

        for(int i = 1; i <= numberOfPages; i++)
        {
            var page = pdfDocument.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);
            result.Add(i, text);
        }

        return result;
    }

    private List<Models.TextChunk> SplitIntoChunks(Dictionary<int, string> pageTexts)
    {
       
        var chunks = new List<SK.FileChatter.Models.TextChunk>();
        int position = 0;

        //foreach (var entry in pageTexts)
        //{
        //    int pageNumber = entry.Key;
        //    string text = entry.Value;

        //    if (string.IsNullOrWhiteSpace(text))
        //        continue;

        //    for (int i = 0; i < text.Length; i += _chunkSize - _chunkOverlapSize)
        //    {
        //        int length = Math.Min(_chunkSize, text.Length - i);
        //        string chunkText = text.Substring(i, length);

        //        chunkcs.Add(new SK.FileChatter.Models.TextChunk
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            Text = chunkText,
        //            PageNumber = pageNumber,
        //            Position = position,
        //            Section = null
        //        });
        //    }
        //}

        foreach(var entry in pageTexts)
        {
            int pageNumber = entry.Key;
            string text = entry.Value;

            // Skip empty pages
            if (string.IsNullOrWhiteSpace(text))
                continue;

            try
            {
                //split into paragraphs using Semantic Kernerl's built-in textchunker

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var lines = TextChunker.SplitPlainTextLines(text, _chunkSize);
                var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, _chunkSize, _chunkOverlapSize);
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                foreach (var paragraph in paragraphs)
                {
                    chunks.Add(new SK.FileChatter.Models.TextChunk
                    {
                        Id = Guid.NewGuid().ToString(),
                        Text = paragraph,
                        PageNumber = pageNumber,
                        Position = position++,
                        Section = null
                    });
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error processing page {PageNumber}: {Message}", pageNumber, ex.Message);
                throw;
            }

        }

        return chunks;
    }
}
