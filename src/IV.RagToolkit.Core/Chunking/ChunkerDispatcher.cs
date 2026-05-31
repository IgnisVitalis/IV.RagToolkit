using Microsoft.Extensions.DependencyInjection;

namespace IV.RagToolkit;

/// <summary>
/// Routes <see cref="Document"/> instances to the <see cref="IChunker{TDocument}"/> registered
/// for that document type. Walks the inheritance chain to find the nearest registered chunker.
/// </summary>
public sealed class ChunkerDispatcher : IChunker
{
    private readonly IServiceProvider _services;

    /// <summary>Initializes a new instance with the service provider used for chunker resolution.</summary>
    public ChunkerDispatcher(IServiceProvider services) => _services = services;

    /// <inheritdoc/>
    public IAsyncEnumerable<Chunk> ChunkAsync(Document document, CancellationToken cancellationToken = default)
    {
        var type = document.GetType();
        while (type is not null && type != typeof(object))
        {
            var adapter = _services.GetKeyedService<IChunkerAdapter>(type);
            if (adapter is not null)
                return adapter.ChunkAsync(document, cancellationToken);
            type = type.BaseType;
        }

        throw new InvalidOperationException(
            $"No chunker registered for '{document.GetType().Name}'. " +
            $"Call AddChunker<{document.GetType().Name}, ...>() during setup.");
    }
}
