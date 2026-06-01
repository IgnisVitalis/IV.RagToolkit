using System.Text.Json;
using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record ChunkDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("chunkIndex")] int? ChunkIndex,
    [property: JsonPropertyName("origin")] OriginDto Origin,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, JsonElement>? Metadata);
