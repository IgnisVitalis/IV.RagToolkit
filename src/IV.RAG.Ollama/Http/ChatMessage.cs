using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);
