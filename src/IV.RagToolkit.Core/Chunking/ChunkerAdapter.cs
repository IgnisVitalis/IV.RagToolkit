namespace IV.RagToolkit;

internal sealed class ChunkerAdapter<TDocument> : IChunkerAdapter where TDocument : Document
{
    private readonly IChunker<TDocument> _inner;

    public ChunkerAdapter(IChunker<TDocument> inner) => _inner = inner;

    public IAsyncEnumerable<Chunk> ChunkAsync(Document document, CancellationToken cancellationToken = default)
        => _inner.ChunkAsync((TDocument)document, cancellationToken);
}
