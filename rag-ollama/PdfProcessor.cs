using RagOllama.Client;
using RagOllama.Data;
using UglyToad.PdfPig;

namespace RagOllama.Processors;

/// <summary>
/// Service responsible for processing PDF documents and storing their content in the vector database
/// </summary>
public class PdfProcessor
{
    private readonly OllamaClient _ollamaClient;
    private readonly SQLiteClient _sqliteClient;

    /// <summary>
    /// Initializes a new instance of the PdfProcessor
    /// </summary>
    /// <param name="ollamaClient">Ollama client for generating embeddings</param>
    /// <param name="sqliteClient">SQLite client for storing document chunks</param>
    public PdfProcessor(OllamaClient ollamaClient, SQLiteClient sqliteClient)
    {
        _ollamaClient = ollamaClient;
        _sqliteClient = sqliteClient;
    }

    /// <summary>
    /// Processes a PDF document by extracting text from each page, generating embeddings,
    /// and storing the chunks in the vector database
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file to process</param>
    /// <param name="embeddingModel">Model to use for generating embeddings</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task ProcessPdfAsync(string pdfPath, string embeddingModel = "bge-large:335m")
    {
        if (!File.Exists(pdfPath))
        {
            Log.Warn($"PDF file not found: {pdfPath}");
            return;
        }

        Log.Debug($"Starting PDF ingestion process: '{pdfPath}'");
        using var doc = PdfDocument.Open(pdfPath);

        for (int i = 0; i < doc.NumberOfPages; i++)
        {
            int page = i + 1;
            string text = doc.GetPage(page).Text ?? string.Empty;
            text = text[..Math.Min(Config.MaxTextLength, text.Length)]; // limit max characters

            if (string.IsNullOrWhiteSpace(text)) continue;

            Log.Debug($"Processing page {page}/{doc.NumberOfPages}: generating embedding");
            float[] embedding = await _ollamaClient.GenerateEmbeddingAsync(text, embeddingModel);

            await _sqliteClient.AddDocumentAsync($"page_{page}", text, embedding);
            Log.Debug($"Successfully processed and stored page {page}/{doc.NumberOfPages}");
        }

        Log.Debug($"Completed PDF ingestion: processed {doc.NumberOfPages} pages total");
    }
} 