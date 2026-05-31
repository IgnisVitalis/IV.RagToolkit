namespace IV.RagToolkit;

internal interface IChunkerAdapter
{
    IAsyncEnumerable<Chunk> ChunkAsync(Document document, CancellationToken cancellationToken = default);
}
