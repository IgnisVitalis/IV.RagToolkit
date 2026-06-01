using System.ComponentModel.DataAnnotations;

namespace IV.RAG;

/// <summary>Options for <see cref="FixedSizeChunker"/>.</summary>
public sealed class FixedSizeChunkerOptions
{
    /// <summary>Maximum number of characters per chunk. Defaults to 512.</summary>
    [Range(1, int.MaxValue)]
    public int ChunkSize { get; set; } = 512;

    /// <summary>
    /// Number of characters shared between consecutive chunks.
    /// Helps preserve context at chunk boundaries. Defaults to 50.
    /// Must be less than <see cref="ChunkSize"/>.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Overlap { get; set; } = 50;

    /// <summary>
    /// When <see langword="true"/>, each chunk ends at the last whitespace character
    /// before <see cref="ChunkSize"/> to avoid cutting mid-word.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool RespectWordBoundaries { get; set; } = true;

    /// <summary>
    /// Chunks shorter than this value are discarded. Defaults to 0 (keep all chunks).
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MinChunkLength { get; set; } = 0;
}
