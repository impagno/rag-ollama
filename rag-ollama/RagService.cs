using RagOllama.Client;
using RagOllama.Data;
using System.Text;

namespace RagOllama.Service;

/// <summary>
/// Service responsible for handling RAG (Retrieval-Augmented Generation) operations
/// </summary>
public class RagService
{
    private readonly SQLiteClient _sqliteClient;
    private readonly OllamaClient _ollamaClient;

    /// <summary>
    /// Initializes a new instance of the RagService
    /// </summary>
    /// <param name="sqliteClient">SQLite client for vector storage operations</param>
    /// <param name="ollamaClient">Ollama client for embedding and text generation</param>
    public RagService(SQLiteClient sqliteClient, OllamaClient ollamaClient)
    {
        _sqliteClient = sqliteClient;
        _ollamaClient = ollamaClient;
    }

    /// <summary>
    /// Processes a question through the RAG pipeline and returns a generated response
    /// </summary>
    /// <param name="question">The question to process</param>
    /// <param name="maxResults">Maximum number of context documents to retrieve</param>
    /// <param name="embeddingModel">Model to use for generating embeddings</param>
    /// <param name="generationModel">Model to use for text generation</param>
    /// <returns>The generated response based on retrieved context</returns>
    public async Task<string> QueryAsync(string question, int maxResults = 3, string embeddingModel = "bge-large:335m", string generationModel = "llama3.2:3b")
    {
        Log.Debug($"Processing RAG query: '{question}'");
        Log.Debug("Generating embedding for question");
        float[] questionEmbedding = await _ollamaClient.GenerateEmbeddingAsync(question, embeddingModel);

        Log.Debug($"Generated embedding with {questionEmbedding.Length} dimensions: [{string.Join(", ", questionEmbedding.Take(5))}...]");

        Log.Debug("Searching for relevant context in SQLite");
        var results = await _sqliteClient.QueryAsync(questionEmbedding, maxResults);
        Log.Debug($"Found {results.Length} relevant document chunks");

        string context = string.Join("\n\n",
            results.Select((r, idx) => $"[{idx + 1}] ID={r.Id}\n{r.Doc.Replace('\n', ' ')}")
        );

        var prompt = BuildPrompt(context, question);

        Console.WriteLine("[PROMPT]\n" + prompt + "\n[/PROMPT]");
        Log.Debug($"Built prompt with {prompt.Length} characters for LLM generation");

        return await _ollamaClient.GenerateCompletionAsync(prompt, generationModel, Config.DefaultMaxTokens, Config.DefaultTemperature);
    }

    /// <summary>
    /// Builds a prompt for the LLM using the retrieved context and question
    /// </summary>
    /// <param name="context">Retrieved context from vector database</param>
    /// <param name="question">User's question</param>
    /// <returns>Formatted prompt for the LLM</returns>
    private string BuildPrompt(string context, string question)
    {
        return new StringBuilder()
            .AppendLine("You are a NASA expert specialized in space exploration.")
            .AppendLine("Use only the context below to answer:\n")
            .AppendLine(context)
            .AppendLine($"\nQUESTION: {question}\n")
            .Append("ANSWER:")
            .ToString();
    }
} 