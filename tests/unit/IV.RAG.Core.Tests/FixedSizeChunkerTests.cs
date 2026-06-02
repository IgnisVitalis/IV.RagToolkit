using FluentAssertions;
using Microsoft.Extensions.Options;

namespace IV.RAG.Tests;

public class FixedSizeChunkerTests
{
    private static FixedSizeChunker Create(
        int chunkSize = 10,
        int overlap = 0,
        bool respectWordBoundaries = false,
        int minChunkLength = 0) =>
        new(Options.Create(new FixedSizeChunkerOptions
        {
            ChunkSize = chunkSize,
            Overlap = overlap,
            RespectWordBoundaries = respectWordBoundaries,
            MinChunkLength = minChunkLength
        }));

    [Fact]
    public async Task ChunkAsync_TextShorterThanChunkSize_ReturnsSingleChunk()
    {
        var chunker = Create(chunkSize: 100);

        var chunks = await chunker.ChunkAsync(new TestDocument("short")).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be("short");
    }

    [Fact]
    public async Task ChunkAsync_TextExactlyChunkSize_ReturnsSingleChunk()
    {
        var chunker = Create(chunkSize: 5);
        var text = "12345";

        var chunks = await chunker.ChunkAsync(new TestDocument(text)).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be(text);
    }

    [Fact]
    public async Task ChunkAsync_NoOverlap_ProducesNonOverlappingChunks()
    {
        var chunker = Create(chunkSize: 5, overlap: 0);

        var chunks = await chunker.ChunkAsync(new TestDocument("1234567890")).ToListAsync();

        chunks.Should().HaveCount(2);
        chunks[0].Text.Should().Be("12345");
        chunks[1].Text.Should().Be("67890");
    }

    [Fact]
    public async Task ChunkAsync_WithOverlap_ConsecutiveChunksShareCharacters()
    {
        // step = 5 - 2 = 3 → positions 0, 3, 6
        var chunker = Create(chunkSize: 5, overlap: 2);

        var chunks = await chunker.ChunkAsync(new TestDocument("1234567890")).ToListAsync();

        chunks[0].Text.Should().Be("12345");
        chunks[1].Text.Should().Be("45678"); // shares "45" with previous
        chunks[2].Text.Should().Be("7890");  // last chunk shorter
    }

    [Fact]
    public async Task ChunkAsync_LastChunk_SmallerWhenTextNotDivisible()
    {
        var chunker = Create(chunkSize: 4, overlap: 0);

        var chunks = await chunker.ChunkAsync(new TestDocument("123456789")).ToListAsync();

        chunks.Last().Text.Should().Be("9");
    }

    [Fact]
    public async Task ChunkAsync_PropagatesDocumentMetadata()
    {
        var chunker = Create(chunkSize: 100);
        var metadata = new Metadata { ["source"] = "doc.txt" };

        var chunks = await chunker.ChunkAsync(new TestDocument("text", metadata: metadata)).ToListAsync();

        chunks[0].Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task ChunkAsync_PropagatesDocumentOrigin()
    {
        var chunker = Create(chunkSize: 100);
        var doc = new TestDocument("text");

        var chunks = await chunker.ChunkAsync(doc).ToListAsync();

        chunks[0].Origin.Should().Be(doc.Source);
    }

    [Fact]
    public async Task ChunkAsync_ChunksHaveNoIdOrEmbedding()
    {
        var chunker = Create(chunkSize: 100);

        var chunks = await chunker.ChunkAsync(new TestDocument("text")).ToListAsync();

        chunks[0].Id.Should().BeNull();
        chunks[0].Embedding.Should().BeNull();
    }

    [Fact]
    public void Constructor_OverlapEqualToChunkSize_Throws()
    {
        var act = () => Create(chunkSize: 5, overlap: 5);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_OverlapGreaterThanChunkSize_Throws()
    {
        var act = () => Create(chunkSize: 5, overlap: 6);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ChunkAsync_RespectWordBoundaries_ChunksDoNotEndMidWord()
    {
        var chunker = Create(chunkSize: 10, overlap: 0, respectWordBoundaries: true);
        // "Hello world" — without word boundary: "Hello worl", "d"
        // with word boundary: "Hello" (cuts at last space before pos 10)
        var chunks = await chunker.ChunkAsync(new TestDocument("Hello world")).ToListAsync();

        chunks.Should().NotContain(c => c.Text.EndsWith(' '));
        chunks[0].Text.Should().Be("Hello");
    }

    [Fact]
    public async Task ChunkAsync_RespectWordBoundariesFalse_CutsAtExactCharacterPosition()
    {
        var chunker = Create(chunkSize: 7, overlap: 0, respectWordBoundaries: false);

        var chunks = await chunker.ChunkAsync(new TestDocument("Hello world")).ToListAsync();

        chunks[0].Text.Should().Be("Hello w");
        chunks[1].Text.Should().Be("orld");
    }

    [Fact]
    public async Task ChunkAsync_MinChunkLength_FiltersChunksBelowThreshold()
    {
        // chunkSize 8, overlap 0 → "12345678", "9" — the last chunk is 1 char
        var chunker = Create(chunkSize: 8, overlap: 0, minChunkLength: 3);

        var chunks = await chunker.ChunkAsync(new TestDocument("123456789")).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be("12345678");
    }
}
