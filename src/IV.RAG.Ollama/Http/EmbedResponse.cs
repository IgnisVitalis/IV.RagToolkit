using System.Text.Json.Serialization;

namespace IV.RAG.Http;

internal sealed record EmbedResponse(
    [property: JsonPropertyName("embeddings")] float[][] Embeddings);
