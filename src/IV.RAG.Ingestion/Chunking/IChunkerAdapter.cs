namespace IV.RAG;

internal interface IChunkerAdapter
{
    IAsyncEnumerable<Chunk> ChunkAsync(Document document, CancellationToken cancellationToken = default);
}
