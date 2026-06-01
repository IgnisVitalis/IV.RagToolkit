using System.ComponentModel.DataAnnotations;

namespace IV.RAG;

/// <summary>Options for <see cref="SentenceChunker"/>.</summary>
public sealed class SentenceChunkerOptions
{
    /// <summary>
    /// Maximum number of characters per chunk. Sentences are accumulated until adding
    /// the next one would exceed this limit. Defaults to 512.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxChunkSize { get; set; } = 512;

    /// <summary>
    /// Chunks shorter than this value are discarded. Defaults to 0 (keep all chunks).
    /// Must not exceed <see cref="MaxChunkSize"/>.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MinChunkLength { get; set; } = 0;
}
