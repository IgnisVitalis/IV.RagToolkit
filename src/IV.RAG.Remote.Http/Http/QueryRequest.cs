using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record QueryRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("topK")] int TopK,
    [property: JsonPropertyName("minScore")] float MinScore);
