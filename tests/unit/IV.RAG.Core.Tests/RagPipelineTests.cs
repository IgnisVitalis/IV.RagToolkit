using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace IV.RAG.Tests;

public class RagPipelineTests
{
    private readonly IIngestionPipeline _ingestion = Substitute.For<IIngestionPipeline>();
    private readonly IRetrievalPipeline _retrieval = Substitute.For<IRetrievalPipeline>();
    private readonly IGenerator _generator = Substitute.For<IGenerator>();
    private readonly RagPipeline _pipeline;

    public RagPipelineTests()
    {
        _pipeline = new RagPipeline(_ingestion, _retrieval, _generator, NullLogger<RagPipeline>.Instance);
    }

    [Fact]
    public async Task IngestAsync_DelegatesToIngestionPipeline()
    {
        var doc = new TestDocument("text");
        await _pipeline.IngestAsync(doc);
        await _ingestion.Received(1).IngestAsync(doc, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryAsync_DelegatesToRetrievalPipeline()
    {
        var options = new RetrievalOptions { TopK = 3 };
        _retrieval.QueryAsync(Arg.Any<string>(), Arg.Any<RetrievalOptions>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SearchResult>());

        await _pipeline.QueryAsync("question", options);

        await _retrieval.Received(1).QueryAsync("question", options, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryAsync_ReturnsResultsFromRetrievalPipeline()
    {
        var origin = new TestDocument("result").Source;
        var expected = new[] { new SearchResult(new Chunk { Text = "result", Origin = origin }, 0.9f) };
        _retrieval.QueryAsync(Arg.Any<string>(), Arg.Any<RetrievalOptions>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var results = await _pipeline.QueryAsync("question");

        results.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task AnswerAsync_RetrievesChunks_ThenCallsGenerator()
    {
        var origin = new TestDocument("result").Source;
        var chunks = new[] { new SearchResult(new Chunk { Text = "result", Origin = origin }, 0.9f) };
        _retrieval.QueryAsync(Arg.Any<string>(), Arg.Any<RetrievalOptions>(), Arg.Any<CancellationToken>())
            .Returns(chunks);
        _generator.GenerateAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<SearchResult>>(), Arg.Any<CancellationToken>())
            .Returns("The answer.");

        var answer = await _pipeline.AnswerAsync("question");

        answer.Should().Be("The answer.");
        await _generator.Received(1).GenerateAsync(
            "question",
            Arg.Is<IReadOnlyList<SearchResult>>(r => r.SequenceEqual(chunks)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnswerAsync_ReturnsGeneratorOutput()
    {
        _retrieval.QueryAsync(Arg.Any<string>(), Arg.Any<RetrievalOptions>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SearchResult>());
        _generator.GenerateAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<SearchResult>>(), Arg.Any<CancellationToken>())
            .Returns("Generated response.");

        var answer = await _pipeline.AnswerAsync("question");

        answer.Should().Be("Generated response.");
    }
}
