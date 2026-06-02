using System.Diagnostics.CodeAnalysis;

namespace IV.RAG.E2ETests.Helpers;

internal sealed record TestDocument : PlainTextDocument
{
    private static readonly Guid TestSourceId = new("a0000000-0000-0000-0000-000000000001");

    [SetsRequiredMembers]
    public TestDocument(
        string text,
        string documentId = "doc-1",
        Metadata? metadata = null)
    {
        Text = text;
        Metadata = metadata;
        Source = new Origin(TestSourceId, "Test", documentId);
    }
}
