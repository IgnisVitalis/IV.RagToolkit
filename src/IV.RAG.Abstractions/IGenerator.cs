namespace IV.RAG;

/// <summary>Generates a natural language answer from a query and retrieved context chunks.</summary>
public interface IGenerator
{
    /// <summary>Generates an answer to <paramref name="query"/> using <paramref name="chunks"/> as context.</summary>
    Task<string> GenerateAsync(string query, IReadOnlyList<SearchResult> chunks, CancellationToken cancellationToken = default);
}
