using RagOllama.Client;
using RagOllama.Data;
using RagOllama.Processors;
using RagOllama.Service;

try
{
    Log.Debug("🚀 Starting RAG Demo Application");

    // Initialize external service clients
    var sqliteClient = new SQLiteClient(Config.SqliteDatabasePath);
    var ollamaClient = new OllamaClient(Config.OllamaUrl);

    // Establish connection to SQLite
    Log.Debug($"🔧 Connecting to SQLite database at {Config.SqliteDatabasePath}");
    await sqliteClient.EnsureDatabaseAsync();
    Log.Debug($"✅ Connected to SQLite database\n");

    // Initialize services
    var pdfProcessor = new PdfProcessor(ollamaClient, sqliteClient);
    var ragService = new RagService(sqliteClient, ollamaClient);

    // Main menu loop
    bool exitApplication = false;
    while (!exitApplication)
    {
        DisplayMenu();
        string choice = Console.ReadLine() ?? string.Empty;

        switch (choice.Trim())
        {
            case "1":
                await ProcessPdfIndexing(pdfProcessor);
                break;
            case "2":
                await ProcessRagQuery(ragService);
                break;
            default:
                Log.Debug("👋 Exiting application. Goodbye!");
                exitApplication = true;
                break;
        }

        if (!exitApplication)
        {
            Log.Debug("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }
}
catch (Exception ex)
{
    Log.Error($"Fatal application error: {ex.Message}");
    Environment.Exit(1);
}

 // Local functions
    void DisplayMenu()
    {
        Console.WriteLine("=== RAG Demo Application ===");
        Console.WriteLine("1) Index PDF to RAG");
        Console.WriteLine("2) Query RAG");
        Console.WriteLine("Any other key to exit");
        Console.Write("\nEnter your choice: ");
    }

    async Task ProcessPdfIndexing(PdfProcessor pdfProcessor)
    {
        try
        {
            Log.Debug("\n📄 PROCESS 1: Processing PDF document...");
            await pdfProcessor.ProcessPdfAsync(Config.PdfPath, Config.EmbeddingModel);
            Log.Debug("✅ PDF processing completed");
        }
        catch (Exception ex)
        {
            Log.Error($"Error processing PDF: {ex.Message}");
        }
    }

    async Task ProcessRagQuery(RagService ragService)
    {
        try
        {
            Log.Debug("\n🔍 Enter your question:");
            Console.Write("> ");
            string question = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(question))
            {
                Log.Warn("❌ Empty question, returning to menu");
                return;
            }

            Log.Debug("🔄 PROCESS 2 & 3: Executing RAG query...");
            string response = await ragService.QueryAsync(
                question, 
                Config.DefaultMaxResults, 
                Config.EmbeddingModel, 
                Config.GenerationModel
            );
            
            Log.Debug($"\n=== RAG Response ===\n{response}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error processing query: {ex.Message}");
        }
    }
    
