using Microsoft.Data.Sqlite;

namespace RagOllama.Data;

/// <summary>
/// Client for interacting with SQLite database for vector storage
/// </summary>
public class SQLiteClient
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the SQLiteClient
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file</param>
    public SQLiteClient(string databasePath)
    {
        _connectionString = $"Data Source={databasePath};";
    }

    /// <summary>
    /// Ensures that the database and table exist, creating them if necessary
    /// </summary>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task EnsureDatabaseAsync()
    {
        await Task.Run(() =>
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                Log.Debug("Connected to SQLite database");

                string createTableSql = $@"
                    CREATE TABLE IF NOT EXISTS {Config.SqliteTableName} (
                        id TEXT PRIMARY KEY,
                        document TEXT NOT NULL,
                        embedding BLOB NOT NULL
                    );
                ";

                using (var command = new SqliteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                    Log.Debug($"Ensured table '{Config.SqliteTableName}' exists");
                }
            }
        });
    }

    /// <summary>
    /// Adds a document and its embedding to the database
    /// </summary>
    /// <param name="id">Unique identifier for the document</param>
    /// <param name="document">Document text content</param>
    /// <param name="embedding">Vector embedding of the document</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task AddDocumentAsync(string id, string document, float[] embedding)
    {
        await Task.Run(() =>
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string insertSql = $@"
                    INSERT OR REPLACE INTO {Config.SqliteTableName} (id, document, embedding)
                    VALUES (@id, @document, @embedding);
                ";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@document", document);
                    command.Parameters.AddWithValue("@embedding", EmbeddingToBlob(embedding));

                    command.ExecuteNonQuery();
                    Log.Debug($"Inserted document '{id}' into database");
                }
            }
        });
    }

    /// <summary>
    /// Queries the database for similar documents using a vector embedding with cosine similarity
    /// </summary>
    /// <param name="queryEmbedding">Vector embedding to search with</param>
    /// <param name="nResults">Number of results to return</param>
    /// <returns>Array of document IDs and their content</returns>
    public async Task<(string Id, string Doc)[]> QueryAsync(float[] queryEmbedding, int nResults = 3)
    {
        return await Task.Run(() =>
        {
            var results = new List<(string Id, string Doc)>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string selectSql = $"SELECT id, document, embedding FROM {Config.SqliteTableName};";

                using (var command = new SqliteCommand(selectSql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var scoredResults = new List<(string Id, string Doc, float Score)>();

                        while (reader.Read())
                        {
                            string id = reader["id"].ToString()!;
                            string doc = reader["document"].ToString()!;
                            byte[] embeddingBlob = (byte[])reader["embedding"];
                            float[] embedding = BlobToEmbedding(embeddingBlob);

                            float similarity = CosineSimilarity(queryEmbedding, embedding);
                            scoredResults.Add((id, doc, similarity));
                        }

                        results = scoredResults
                            .OrderByDescending(r => r.Score)
                            .Take(nResults)
                            .Select(r => (r.Id, r.Doc))
                            .ToList();

                        Log.Debug($"Query returned {results.Count} results");
                    }
                }
            }

            return results.ToArray();
        });
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors
    /// </summary>
    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0f;

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0f;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Converts a float array to a binary blob for storage
    /// </summary>
    private byte[] EmbeddingToBlob(float[] embedding)
    {
        byte[] blob = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, blob, 0, blob.Length);
        return blob;
    }

    /// <summary>
    /// Converts a binary blob back to a float array
    /// </summary>
    private float[] BlobToEmbedding(byte[] blob)
    {
        float[] embedding = new float[blob.Length / sizeof(float)];
        Buffer.BlockCopy(blob, 0, embedding, 0, blob.Length);
        return embedding;
    }
}
