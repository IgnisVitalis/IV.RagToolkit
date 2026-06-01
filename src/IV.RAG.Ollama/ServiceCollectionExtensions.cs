using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IV.RAG;

/// <summary>DI registration extensions for IV.RAG.Ollama.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OllamaEmbedder"/> as the <see cref="IEmbedder"/> implementation.
    /// </summary>
    public static RAGBuilder AddOllamaEmbedder(
        this RAGBuilder builder,
        Action<OllamaOptions>? configure = null)
    {
        if (configure is not null)
            builder.Services.Configure<OllamaOptions>(configure);
        else
            builder.Services.AddOptions<OllamaOptions>();

        builder.Services.AddHttpClient("IV.RAG.Ollama")
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
                client.BaseAddress = new Uri(options.Endpoint);
            });

        builder.Services.AddSingleton<IEmbedder, OllamaEmbedder>();
        return builder;
    }

    /// <summary>
    /// Registers <see cref="OllamaGenerator"/> as the <see cref="IGenerator"/> implementation.
    /// </summary>
    public static RAGBuilder AddOllamaGenerator(
        this RAGBuilder builder,
        Action<OllamaOptions>? configure = null)
    {
        if (configure is not null)
            builder.Services.Configure<OllamaOptions>(configure);
        else
            builder.Services.AddOptions<OllamaOptions>();

        builder.Services.AddHttpClient("IV.RAG.Ollama")
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
                client.BaseAddress = new Uri(options.Endpoint);
            });

        builder.Services.AddSingleton<IGenerator, OllamaGenerator>();
        return builder;
    }
}
