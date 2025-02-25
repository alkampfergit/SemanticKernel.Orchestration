using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Configuration;
using System.ComponentModel;

namespace SemanticKernel.Orchestration.SampleAgents.KernelMemory;

public class KernelMemoryAssistant : BaseAssistant
{
    private MemoryServerless _kernelMemory;

    public KernelMemoryAssistant() : base("KernelMemoryAssistant")
    {
        var sc = new ServiceCollection();
        var builder = CreateBasicKernelMemoryBuilder(sc);
        _kernelMemory = builder.Build<MemoryServerless>();

        RegisterFunctionDelegate(
            "IndexDocuments",
            KernelFunctionFactory.CreateFromMethod(IndexDocuments),
            async (args) => await IndexDocuments(
                args["path"].ToString()!,
                args["filter"].ToString()!));
    }

    [Description("Index a single document or an entire directory")]
    private async Task<AssistantResponse> IndexDocuments(string path, string filter)
    {
        //Check if path is a single file or a directory
        if (File.Exists(path))
        {
            await IndexDocument(path);
            return new AssistantResponse($"Document {path} indexed");
        }
        else if (Directory.Exists(path))
        {
            filter = string.IsNullOrEmpty(filter) ? "*.*" : filter;
            var files = Directory.GetFiles(path, filter);
            foreach (var file in files)
            {
                await IndexDocument(file);
            }
        }

        return new AssistantResponse($"Path {path} not found");
    }

    private async Task IndexDocument(string path)
    {
        _logger.LogInformation("Indexing document {Path}", path);
        await _kernelMemory.ImportDocumentAsync(path);
        _logger.LogInformation("Document {Path} indexed", path);
    }

    private IKernelMemoryBuilder CreateBasicKernelMemoryBuilder(
        ServiceCollection services)
    {
        // we need a series of services to use Kernel Memory, the first one is
        // an embedding service that will be used to create dense vector for
        // pieces of test. We can use standard ADA embedding service
        var embeddingConfig = new AzureOpenAIConfig
        {
            APIKey = Dotenv.Get("OPENAI_API_KEY"),
            Deployment = "text-embedding-ada-002",
            Endpoint = Dotenv.Get("AZURE_ENDPOINT"),
            APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
            Auth = AzureOpenAIConfig.AuthTypes.APIKey
        };

        // Now kenel memory needs the LLM data to be able to pass question
        // and retreived segments to the model. We can Use GPT35
        var chatConfig = new AzureOpenAIConfig
        {
            APIKey = Dotenv.Get("OPENAI_API_KEY"),
            Deployment = Dotenv.Get("KERNEL_MEMORY_DEPLOYMENT_NAME"),
            Endpoint = Dotenv.Get("AZURE_ENDPOINT"),
            APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
            Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            MaxTokenTotal = 4096
        };

        var kernelMemoryBuilder = new KernelMemoryBuilder(services)
            .WithAzureOpenAITextGeneration(chatConfig)
            .WithAzureOpenAITextEmbeddingGeneration(embeddingConfig);

        kernelMemoryBuilder
           .WithSimpleFileStorage(new SimpleFileStorageConfig()
           {
               Directory = "c:\\temp\\km\\storage",
               StorageType = FileSystemTypes.Disk
           })
           .WithSimpleVectorDb(new SimpleVectorDbConfig()
           {
               Directory = "c:\\temp\\km\\vectorstorage",
               StorageType = FileSystemTypes.Disk
           });

        services.AddSingleton<IKernelMemoryBuilder>(kernelMemoryBuilder);
        return kernelMemoryBuilder;
    }
}
