using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record QueryResponse(
    [property: JsonPropertyName("results")] SearchResultDto[] Results);
