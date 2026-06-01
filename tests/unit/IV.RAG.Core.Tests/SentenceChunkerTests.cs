using FluentAssertions;
using Microsoft.Extensions.Options;

namespace IV.RAG.Tests;

public class SentenceChunkerTests
{
    private static SentenceChunker Create(int maxChunkSize = 512, int minChunkLength = 0) =>
        new(Options.Create(new SentenceChunkerOptions
        {
            MaxChunkSize = maxChunkSize,
            MinChunkLength = minChunkLength
        }));

    [Fact]
    public async Task ChunkAsync_ShortText_ReturnsSingleChunk()
    {
        var chunker = Create(maxChunkSize: 100);

        var chunks = await chunker.ChunkAsync(new TestDocument("Hello world.")).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be("Hello world.");
    }

    [Fact]
    public async Task ChunkAsync_AccumulatesSentences_UntilMaxChunkSize()
    {
        var chunker = Create(maxChunkSize: 30);
        // "Hello world." (12) + " " + "How are you?" (12) = 25 chars → fits
        // + " " + "Fine thanks." (12) = 38 → exceeds → new chunk
        var doc = new TestDocument("Hello world. How are you? Fine thanks.");

        var chunks = await chunker.ChunkAsync(doc).ToListAsync();

        chunks.Should().HaveCount(2);
        chunks[0].Text.Should().Be("Hello world. How are you?");
        chunks[1].Text.Should().Be("Fine thanks.");
    }

    [Fact]
    public async Task ChunkAsync_SingleSentenceExceedsMaxChunkSize_YieldsAsIs()
    {
        var chunker = Create(maxChunkSize: 5);
        var doc = new TestDocument("This is a very long sentence that exceeds the limit.");

        var chunks = await chunker.ChunkAsync(doc).ToListAsync();

        chunks.Should().HaveCount(1);
        chunks[0].Text.Should().Be("This is a very long sentence that exceeds the limit.");
    }

    [Fact]
    public async Task ChunkAsync_ParagraphBreaks_SplitIntoChunks()
    {
        var chunker = Create(maxChunkSize: 200);
        var doc = new TestDocument("First paragraph.\n\nSecond paragraph.");

        var chunks = await chunker.ChunkAsync(doc).ToListAsync();

        chunks.Should().HaveCount(2);
        chunks[0].Text.Should().Be("First paragraph.");
        chunks[1].Text.Should().Be("Second paragraph.");
    }

    [Fact]
    public async Task ChunkAsync_MinChunkLength_FiltersShortChunks()
    {
        var chunker = Create(maxChunkSize: 200, minChunkLength: 20);
        // "Hi." is 3 chars — below threshold
        var doc = new TestDocument("Hi. This is a longer sentence that should pass.");

        var chunks = await chunker.ChunkAsync(doc).ToListAsync();

        chunks.Should().NotContain(c => c.Text.Length < 20);
    }

    [Fact]
    public async Task ChunkAsync_PropagatesOriginAndMetadata()
    {
        var chunker = Create(maxChunkSize: 100);
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        var doc = new TestDocument("Hello world.", metadata: metadata);

        var chunks = await chunker.ChunkAsync(doc).ToListAsync();

        chunks[0].Origin.Should().Be(doc.Source);
        chunks[0].Metadata.Should().BeEquivalentTo(metadata);
    }
}
