using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace IV.RagToolkit;

/// <summary>
/// Splits a <see cref="PlainTextDocument"/> at sentence boundaries, accumulating sentences
/// into chunks up to <see cref="SentenceChunkerOptions.MaxChunkSize"/> characters.
/// Paragraph breaks (<c>\n\n</c>) are always hard boundaries — a chunk never spans two paragraphs.
/// Sentence boundaries within a paragraph are detected at <c>.</c>, <c>!</c>, or <c>?</c>
/// followed by whitespace. A single sentence that exceeds
/// <see cref="SentenceChunkerOptions.MaxChunkSize"/> is yielded as-is.
/// </summary>
public sealed class SentenceChunker : IChunker<PlainTextDocument>
{
    private static readonly Regex ParagraphSplitter =
        new(@"\n{2,}", RegexOptions.Compiled);
    private static readonly Regex SentenceSplitter =
        new(@"(?<=[.!?])\s+", RegexOptions.Compiled);

    private readonly SentenceChunkerOptions _options;

    /// <summary>Initializes a new instance with the provided options.</summary>
    public SentenceChunker(IOptions<SentenceChunkerOptions> options) => _options = options.Value;

#pragma warning disable CS1998
    /// <inheritdoc/>
    public async IAsyncEnumerable<Chunk> ChunkAsync(
        PlainTextDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var paragraphs = ParagraphSplitter
            .Split(document.Text)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);

        foreach (var paragraph in paragraphs)
        {
            var sentences = SentenceSplitter
                .Split(paragraph)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

            var buffer = new StringBuilder();

            foreach (var sentence in sentences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (buffer.Length > 0 && buffer.Length + 1 + sentence.Length > _options.MaxChunkSize)
                {
                    if (buffer.Length >= _options.MinChunkLength)
                        yield return MakeChunk(document, buffer.ToString());
                    buffer.Clear();
                }

                if (buffer.Length > 0) buffer.Append(' ');
                buffer.Append(sentence);

                // Single sentence exceeds limit — yield immediately
                if (buffer.Length > _options.MaxChunkSize)
                {
                    if (buffer.Length >= _options.MinChunkLength)
                        yield return MakeChunk(document, buffer.ToString());
                    buffer.Clear();
                }
            }

            if (buffer.Length >= _options.MinChunkLength && buffer.Length > 0)
                yield return MakeChunk(document, buffer.ToString());
            buffer.Clear();
        }
    }
#pragma warning restore CS1998

    private static Chunk MakeChunk(PlainTextDocument document, string text) =>
        new() { Text = text, Metadata = document.Metadata, Origin = document.Source };
}
