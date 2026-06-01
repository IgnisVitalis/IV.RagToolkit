namespace IV.RAG;

/// <summary>Handles similarity search: embed query → retrieve ranked chunks.</summary>
public interface IRetrievalPipeline
{
    /// <summary>Embeds <paramref name="query"/> and returns the most relevant chunks according to <paramref name="options"/>.</summary>
    Task<IReadOnlyList<SearchResult>> QueryAsync(string query, RetrievalOptions? options = null, CancellationToken cancellationToken = default);
}
