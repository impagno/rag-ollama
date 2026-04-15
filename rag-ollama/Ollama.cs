using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

/// <summary>
/// Client for interacting with the Ollama API for text embeddings and generation
/// </summary>
public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _embeddingsUrl;
    private readonly string _completionsUrl;

    /// <summary>
    /// Initializes a new instance of the OllamaClient
    /// </summary>
    /// <param name="baseUrl">Base URL of the Ollama API</param>
    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl;
        _embeddingsUrl = $"{baseUrl}/api/embeddings";
        _completionsUrl = $"{baseUrl}/api/generate";
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// Generates an embedding vector for the given text using the specified model
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="model">Model to use for embedding generation</param>
    /// <returns>Array of floats representing the text embedding</returns>
    public async Task<float[]> GenerateEmbeddingAsync(string text, string model = "bge-large:335m")
    {
        Log.Debug($"Generating embedding using model '{model}'");
        var body = new { model = model, prompt = text };
        using var resp = await _http.PostAsJsonAsync(_embeddingsUrl, body);

        string content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            Log.Error($"Embedding generation failed: {resp.StatusCode}\nRESPONSE:\n{content}");
            throw new Exception("Failed to generate embedding.");
        }

        using var doc = JsonDocument.Parse(content);
        var arr = doc.RootElement.GetProperty("embedding").EnumerateArray().Select(e => e.GetSingle()).ToArray();
        Log.Debug($"Successfully generated embedding vector with {arr.Length} dimensions");
        return arr;
    }

    /// <summary>
    /// Generates a text completion using the specified model and parameters
    /// </summary>
    /// <param name="prompt">Input prompt for text generation</param>
    /// <param name="model">Model to use for text generation</param>
    /// <param name="maxTokens">Maximum number of tokens to generate</param>
    /// <param name="temperature">Temperature parameter for generation (0.0 = deterministic)</param>
    /// <returns>Generated text completion</returns>
    public async Task<string> GenerateCompletionAsync(string prompt, string model = "llama3.2:3b", int maxTokens = 256, double temperature = 0.0)
    {
        Log.Debug($"Starting text generation with model '{model}' (max_tokens={maxTokens}, temp={temperature})");
        try
        {
            var compPayload = new
            {
                model = model,
                prompt = prompt,
                max_tokens = maxTokens,
                temperature = temperature,
                stream = true
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, _completionsUrl)
            {
                Content = JsonContent.Create(compPayload)
            };
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var sr = new StreamReader(stream);
            var full = new StringBuilder();
            var sw = Stopwatch.StartNew();

            while (!sr.EndOfStream)
            {
                var line = await sr.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                using var partDoc = JsonDocument.Parse(line);
                var part = partDoc.RootElement;
                var chunk = part.GetProperty("response").GetString() ?? string.Empty;
                var done = part.GetProperty("done").GetBoolean();

                full.Append(chunk);
                Log.Debug($"Received text chunk: {chunk.Length} chars (done={done})");

                if (done) break;
            }

            sw.Stop();
            Log.Debug($"Completed text generation in {sw.Elapsed.TotalSeconds:F2}s");
            return full.ToString();
        }
        catch (TaskCanceledException)
        {
            Log.Error("Completion error: timeout exceeded");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Completion error: {ex.Message}");
            throw;
        }
    }
}