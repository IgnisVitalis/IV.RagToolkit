using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record ChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] ChatMessage[] Messages,
    [property: JsonPropertyName("stream")] bool Stream = false);
