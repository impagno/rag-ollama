/// <summary>
/// Centralized configuration class for the RAG application
/// Contains all constants and configuration values
/// </summary>
static class Config
{
    // SQLite Configuration
    /// <summary>
    /// SQLite database file path
    /// </summary>
    public const string SqliteDatabasePath = "rag_embeddings.db";

    /// <summary>
    /// SQLite table name for storing documents
    /// </summary>
    public const string SqliteTableName = "documents";

    // Ollama Configuration
    /// <summary>
    /// Ollama API base URL
    /// </summary>
    public const string OllamaUrl = "http://localhost:11434";
    
    /// <summary>
    /// Default embedding model for generating document embeddings
    /// </summary>
    public const string EmbeddingModel = "bge-large:335m";
    
    /// <summary>
    /// Default generation model for text completion
    /// </summary>
    public const string GenerationModel = "llama3.2:3b";

    // File Configuration
    /// <summary>
    /// Path to the PDF file to be processed
    /// </summary>
    public const string PdfPath = @"pdf/artemis2.pdf";

    // RAG Configuration
    /// <summary>
    /// Default number of context documents to retrieve for RAG queries
    /// </summary>
    public const int DefaultMaxResults = 3;
    
    /// <summary>
    /// Default maximum tokens for text generation
    /// </summary>
    public const int DefaultMaxTokens = 256;
    
    /// <summary>
    /// Default temperature for text generation (0.0 = deterministic)
    /// </summary>
    public const double DefaultTemperature = 0.0;
    
    /// <summary>
    /// Maximum text length for document chunks
    /// </summary>
    public const int MaxTextLength = 1000;
} 