using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.Memory.Merkon;
using Microsoft.SemanticKernel.Plugins.Memory;

namespace TestMerkonDB
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = "[OPEN AI KEY HERE]";//"-- Open AI Key --";
            const string memoryCollectionName = "SKGitHub";

            var githubFiles = new Dictionary<string, string>()
            {
                ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
                    = "README: Installation, getting started, and how to contribute",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
                    = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/00-getting-started.ipynb"]
                    = "Jupyter notebook describing how to get started with the Semantic Kernel",
                ["https://github.com/microsoft/semantic-kernel/tree/main/samples/plugins/ChatPlugin/ChatGPT"]
                    = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
                ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Memory/VolatileMemoryStore.cs"]
                    = "C# class that defines a volatile embedding store",
                ["https://github.com/microsoft/semantic-kernel/tree/main/samples/dotnet/KernelHttpServer/README.md"]
                    = "README: How to set up a Semantic Kernel Service API using Azure Function Runtime v4",
                ["https://github.com/microsoft/semantic-kernel/tree/main/samples/apps/chat-summary-webapp-react/README.md"]
                    = "README: README associated with a sample starter react-based chat summary webapp",
            };
            var memoryBuilder = new MemoryBuilder();


            memoryBuilder.WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", apiKey);


            var merkonMemoryStore = new MerkonMemoryStore("github");

            memoryBuilder.WithMemoryStore(merkonMemoryStore);

            var memory = memoryBuilder.Build();
            Console.WriteLine("Adding some GitHub file URLs and their descriptions to Merkon Semantic Memory.");
            var i = 0;
            foreach (var entry in githubFiles)
            {
                await memory.SaveReferenceAsync(
                    collection: memoryCollectionName,
                    description: entry.Value,
                    text: entry.Value,
                    externalId: entry.Key,
                    externalSourceName: "GitHub"
                );
                Console.WriteLine($"  URL {++i} saved");
            }
            string ask = "I love Jupyter notebooks, how should I get started?";
            Console.WriteLine("===========================\n" +
                                "Query: " + ask + "\n");

            var memories = memory.SearchAsync(memoryCollectionName, ask, limit: 5, minRelevanceScore: 0.6);

            i = 0;
            await foreach (var mem in memories)
            {
                Console.WriteLine($"Result {++i}:");
                Console.WriteLine("  URL:     : " + mem.Metadata.Id);
                Console.WriteLine("  Title    : " + mem.Metadata.Description);
                Console.WriteLine("  Relevance: " + mem.Relevance);
                Console.WriteLine();
            }

            Console.ReadLine();
        }
    }
}