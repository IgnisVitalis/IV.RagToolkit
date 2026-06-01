using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record ChatResponse(
    [property: JsonPropertyName("message")] ChatMessage Message);
