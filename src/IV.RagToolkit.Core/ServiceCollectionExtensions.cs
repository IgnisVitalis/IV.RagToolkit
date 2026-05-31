using Microsoft.Extensions.DependencyInjection;

namespace IV.RagToolkit;

/// <summary>DI registration extensions for IV.RagToolkit.Core.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core RAG pipeline and the chunker dispatcher, and returns a builder
    /// for chaining provider registrations.
    /// </summary>
    public static RagToolkitBuilder AddRagToolkit(this IServiceCollection services)
    {
        services.AddSingleton<IRagPipeline, RagPipeline>();
        services.AddSingleton<IChunker, ChunkerDispatcher>();
        return new RagToolkitBuilder(services);
    }

    /// <summary>
    /// Registers a typed chunker for <typeparamref name="TDocument"/>.
    /// </summary>
    public static RagToolkitBuilder AddChunker<TDocument, TChunker>(this RagToolkitBuilder builder)
        where TDocument : Document
        where TChunker : class, IChunker<TDocument>
    {
        builder.Services.AddSingleton<IChunker<TDocument>, TChunker>();
        builder.Services.AddKeyedSingleton<IChunkerAdapter>(
            typeof(TDocument),
            (sp, _) => new ChunkerAdapter<TDocument>(sp.GetRequiredService<IChunker<TDocument>>()));
        return builder;
    }

    /// <summary>
    /// Registers a typed chunker with options for <typeparamref name="TDocument"/>.
    /// Options validation is the caller's responsibility.
    /// </summary>
    public static RagToolkitBuilder AddChunker<TDocument, TChunker, TOptions>(
        this RagToolkitBuilder builder,
        Action<TOptions>? configure = null)
        where TDocument : Document
        where TChunker : class, IChunker<TDocument>
        where TOptions : class
    {
        var optionsBuilder = builder.Services.AddOptions<TOptions>();
        if (configure is not null)
            optionsBuilder.Configure(configure);
        return builder.AddChunker<TDocument, TChunker>();
    }

    /// <summary>
    /// Registers <see cref="FixedSizeChunker"/> for <see cref="PlainTextDocument"/>.
    /// </summary>
    public static RagToolkitBuilder AddPlainTextChunker(
        this RagToolkitBuilder builder,
        Action<FixedSizeChunkerOptions>? configure = null)
    {
        builder.Services.AddOptions<FixedSizeChunkerOptions>()
            .Configure(configure ?? (_ => { }))
            .Validate(o => o.ChunkSize >= 1, "ChunkSize must be at least 1.")
            .Validate(o => o.Overlap >= 0, "Overlap must be non-negative.")
            .Validate(o => o.Overlap < o.ChunkSize, "Overlap must be less than ChunkSize.")
            .Validate(o => o.MinChunkLength >= 0, "MinChunkLength must be non-negative.")
            .ValidateOnStart();
        return builder.AddChunker<PlainTextDocument, FixedSizeChunker>();
    }

    /// <summary>
    /// Registers <see cref="SentenceChunker"/> for <see cref="PlainTextDocument"/>.
    /// </summary>
    public static RagToolkitBuilder AddSentenceChunker(
        this RagToolkitBuilder builder,
        Action<SentenceChunkerOptions>? configure = null)
    {
        builder.Services.AddOptions<SentenceChunkerOptions>()
            .Configure(configure ?? (_ => { }))
            .Validate(o => o.MaxChunkSize >= 1, "MaxChunkSize must be at least 1.")
            .Validate(o => o.MinChunkLength >= 0, "MinChunkLength must be non-negative.")
            .Validate(o => o.MinChunkLength <= o.MaxChunkSize, "MinChunkLength must not exceed MaxChunkSize.")
            .ValidateOnStart();
        return builder.AddChunker<PlainTextDocument, SentenceChunker>();
    }

}
