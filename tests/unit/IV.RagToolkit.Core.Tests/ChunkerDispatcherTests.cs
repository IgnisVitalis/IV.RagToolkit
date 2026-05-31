using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace IV.RagToolkit.Tests;

public class ChunkerDispatcherTests
{
    private static readonly Document.Origin TestOrigin =
        new(new Guid("a0000000-0000-0000-0000-000000000001"), "Test", "doc-1");

    private static IChunker BuildDispatcher(Action<RagToolkitBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddRagToolkit();
        configure?.Invoke(builder);
        return services.BuildServiceProvider().GetRequiredService<IChunker>();
    }

    [Fact]
    public async Task ChunkAsync_PlainTextDocument_RoutesToFixedSizeChunker()
    {
        var dispatcher = BuildDispatcher(b => b.AddPlainTextChunker(o => o.ChunkSize = 100));
        var doc = new PlainTextDocument { Source = TestOrigin, Text = "hello world" };

        var chunks = await dispatcher.ChunkAsync(doc).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be("hello world");
        chunks[0].Origin.Should().Be(TestOrigin);
    }

    [Fact]
    public async Task ChunkAsync_DerivedDocumentType_WalksInheritanceChain()
    {
        var dispatcher = BuildDispatcher(b => b.AddPlainTextChunker(o => o.ChunkSize = 100));

        // TestDocument inherits PlainTextDocument — no dedicated chunker registered for it
        var doc = new TestDocument("hello world");

        var chunks = await dispatcher.ChunkAsync(doc).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be("hello world");
    }

    [Fact]
    public void ChunkAsync_UnregisteredDocumentType_ThrowsWithDocumentTypeName()
    {
        var dispatcher = BuildDispatcher(); // no chunkers registered
        var doc = new PlainTextDocument { Source = TestOrigin, Text = "hello" };

        Action act = () => dispatcher.ChunkAsync(doc);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PlainTextDocument*");
    }
}
