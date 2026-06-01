using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace IV.RAG;

/// <summary>
/// Splits a <see cref="PlainTextDocument"/> into fixed-size character chunks with optional overlap.
/// </summary>
public sealed class FixedSizeChunker : IChunker<PlainTextDocument>
{
    private readonly FixedSizeChunkerOptions _options;

    /// <summary>Initializes a new instance with the provided options.</summary>
    public FixedSizeChunker(IOptions<FixedSizeChunkerOptions> options)
    {
        var value = options.Value;
        if (value.Overlap >= value.ChunkSize)
            throw new InvalidOperationException($"{nameof(FixedSizeChunkerOptions.Overlap)} must be less than {nameof(FixedSizeChunkerOptions.ChunkSize)}.");
        _options = value;
    }

#pragma warning disable CS1998
    /// <inheritdoc/>
    public async IAsyncEnumerable<Chunk> ChunkAsync(
        PlainTextDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = document.Text;
        var chunkSize = _options.ChunkSize;
        var step = chunkSize - _options.Overlap;
        var position = 0;

        while (position < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var length = Math.Min(chunkSize, text.Length - position);

            if (_options.RespectWordBoundaries && position + length < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', position + length - 1, length);
                if (lastSpace > position)
                    length = lastSpace - position;
            }

            var chunkText = text.Substring(position, length);
            if (chunkText.Length >= _options.MinChunkLength)
                yield return new Chunk { Text = chunkText, Metadata = document.Metadata, Origin = document.Source };

            position += step;
        }
    }
#pragma warning restore CS1998
}
