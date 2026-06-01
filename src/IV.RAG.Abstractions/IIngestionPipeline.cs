namespace IV.RAG;

/// <summary>Handles document ingestion: chunk → embed → store.</summary>
public interface IIngestionPipeline
{
    /// <summary>Chunks, embeds, and stores <paramref name="document"/>.</summary>
    Task IngestAsync(Document document, CancellationToken cancellationToken = default);
}
