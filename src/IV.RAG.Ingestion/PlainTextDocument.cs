namespace IV.RAG;

/// <summary>A plain-text document for ingestion.</summary>
public record PlainTextDocument : Document
{
    /// <summary>Text content of the document.</summary>
    public required string Text { get; init; }
}
