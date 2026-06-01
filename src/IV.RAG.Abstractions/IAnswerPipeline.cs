namespace IV.RAG;

/// <summary>Handles the full answer loop: retrieve relevant chunks, then generate a natural language answer.</summary>
public interface IAnswerPipeline
{
    /// <summary>Retrieves relevant chunks for <paramref name="query"/> and returns a generated answer.</summary>
    Task<string> AnswerAsync(string query, RetrievalOptions? options = null, CancellationToken cancellationToken = default);
}
