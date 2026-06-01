namespace IV.RAG;

/// <summary>
/// Full local RAG pipeline: chunk → embed → store (ingest), embed → retrieve (query), retrieve → generate (answer).
/// </summary>
public interface IRagPipeline : IIngestionPipeline, IRetrievalPipeline, IAnswerPipeline
{
}
