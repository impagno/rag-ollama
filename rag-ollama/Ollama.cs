using System.Diagnostics;
using System.Text;
using OllamaSharp;
using OllamaSharp.Models;

namespace RagOllama.Client;

/// <summary>
/// Client for interacting with the Ollama API for text embeddings and generation
/// </summary>
public class OllamaClient
{
    private readonly OllamaApiClient _client;

    /// <summary>
    /// Initializes a new instance of the OllamaClient
    /// </summary>
    /// <param name="baseUrl">Base URL of the Ollama API</param>
    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(10) // Set your timeout here
        };

        _client = new OllamaApiClient(httpClient);
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
        try
        {
            _client.SelectedModel = model;
            var response = await _client.EmbedAsync(text);
            var embedding = response.Embeddings.FirstOrDefault();

            if (embedding is null)
            {
                throw new Exception("Embedding response was empty.");
            }

            var arr = embedding.ToArray();
            Log.Debug($"Successfully generated embedding vector with {arr.Length} dimensions");
            return arr;
        }
        catch (Exception ex)
        {
            Log.Error($"Embedding generation failed: {ex.Message}");
            throw;
        }
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
            var full = new StringBuilder();
            var sw = Stopwatch.StartNew();
            var request = new GenerateRequest
            {
                Model = model,
                Prompt = prompt,
                Stream = true,
                Options = new RequestOptions
                {
                    NumPredict = maxTokens,
                    Temperature = (float)temperature
                }
            };

            await foreach (var chunk in _client.GenerateAsync(request))
            {
                var text = chunk?.Response ?? string.Empty;
                if (text.Length == 0)
                {
                    continue;
                }

                full.Append(text);
                Log.Debug($"Received text chunk: {text.Length} chars");
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