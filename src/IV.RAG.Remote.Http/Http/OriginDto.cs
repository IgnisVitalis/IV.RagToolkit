using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record OriginDto(
    [property: JsonPropertyName("sourceId")] Guid SourceId,
    [property: JsonPropertyName("documentType")] string DocumentType,
    [property: JsonPropertyName("documentId")] string DocumentId);
