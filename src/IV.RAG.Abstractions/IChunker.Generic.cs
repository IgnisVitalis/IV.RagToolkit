namespace IV.RAG;

/// <summary>Splits a <typeparamref name="TDocument"/> into <see cref="Chunk"/> instances.</summary>
/// <typeparam name="TDocument">The document type this chunker handles.</typeparam>
public interface IChunker<TDocument> where TDocument : Document
{
    /// <summary>Splits <paramref name="document"/> into chunks, yielding each as it is produced.</summary>
    IAsyncEnumerable<Chunk> ChunkAsync(TDocument document, CancellationToken cancellationToken = default);
}
