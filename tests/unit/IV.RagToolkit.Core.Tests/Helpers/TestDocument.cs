using System.Diagnostics.CodeAnalysis;

namespace IV.RagToolkit.Tests;

internal sealed record TestDocument : PlainTextDocument
{
    private static readonly Guid TestSourceId = new("a0000000-0000-0000-0000-000000000001");

    [SetsRequiredMembers]
    public TestDocument(
        string text,
        string documentId = "doc-1",
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        Text = text;
        Metadata = metadata;
        Source = new Origin(TestSourceId, "Test", documentId);
    }
}
