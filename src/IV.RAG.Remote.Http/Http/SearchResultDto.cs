using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record SearchResultDto(
    [property: JsonPropertyName("chunk")] ChunkDto Chunk,
    [property: JsonPropertyName("score")] float Score);
