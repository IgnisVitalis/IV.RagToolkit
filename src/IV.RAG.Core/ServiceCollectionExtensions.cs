using Microsoft.Extensions.DependencyInjection;

namespace IV.RAG;

/// <summary>DI registration extensions for IV.RAG.Core.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full local RAG pipeline (<see cref="IRagPipeline"/>, <see cref="IAnswerPipeline"/>)
    /// backed by <see cref="RetrievalPipeline"/> and <see cref="RagPipeline"/>.
    /// Chain <c>.AddXxxChunker()</c>, <c>.AddXxxEmbedder()</c>, <c>.AddXxxVectorStore()</c>,
    /// and <c>.AddXxxGenerator()</c> to complete the setup.
    /// </summary>
    public static RAGBuilder AddRagToolkit(this IServiceCollection services)
    {
        services.AddSingleton<RetrievalPipeline>();
        services.AddSingleton<IIngestionPipeline>(sp => sp.GetRequiredService<RetrievalPipeline>());
        services.AddSingleton<IRetrievalPipeline>(sp => sp.GetRequiredService<RetrievalPipeline>());
        services.AddSingleton<IRagPipeline, RagPipeline>();
        services.AddSingleton<IAnswerPipeline>(sp => sp.GetRequiredService<IRagPipeline>());
        return new RAGBuilder(services);
    }

    /// <summary>
    /// Registers the server-side retrieval pipeline (<see cref="IIngestionPipeline"/>,
    /// <see cref="IRetrievalPipeline"/>) backed by <see cref="RetrievalPipeline"/>.
    /// Chain <c>.AddXxxChunker()</c>, <c>.AddXxxEmbedder()</c>, and <c>.AddXxxVectorStore()</c>
    /// to complete the setup.
    /// </summary>
    public static RAGBuilder AddRetrievalPipeline(this IServiceCollection services)
    {
        services.AddSingleton<RetrievalPipeline>();
        services.AddSingleton<IIngestionPipeline>(sp => sp.GetRequiredService<RetrievalPipeline>());
        services.AddSingleton<IRetrievalPipeline>(sp => sp.GetRequiredService<RetrievalPipeline>());
        return new RAGBuilder(services);
    }

    /// <summary>
    /// Registers the client-side answer pipeline (<see cref="IAnswerPipeline"/>) backed by
    /// <see cref="AnswerPipeline"/>. Chain <c>.AddXxxRetrievalPipeline()</c> and
    /// <c>.AddXxxGenerator()</c> to complete the setup.
    /// </summary>
    public static RAGBuilder AddAnswerPipeline(this IServiceCollection services)
    {
        services.AddSingleton<IAnswerPipeline, AnswerPipeline>();
        return new RAGBuilder(services);
    }
}
