# IV.RagToolkit

A composable .NET 9 toolkit for building RAG (Retrieval-Augmented Generation) pipelines. Provides infrastructure and base abstractions — every step is swappable via dependency injection without touching pipeline logic.

> **Pre-1.0 — active development. Breaking changes may occur between versions.**

## Packages

| Package | Description |
|---|---|
| `IV.RagToolkit.Abstractions` | Core interfaces and models. No dependencies. Domain projects depend only on this. |
| `IV.RagToolkit.Core` | Pipeline orchestration, built-in document types, and chunkers. Depends only on Abstractions. |
| `IV.RagToolkit.Ollama` | `IEmbedder` backed by the Ollama `/api/embed` endpoint. |
| `IV.RagToolkit.Postgres` | `IVectorStore` and `IRetriever` backed by PostgreSQL + pgvector. |

## Quick start

### 1. Register services

```csharp
services.AddRagToolkit()
    .AddPlainTextChunker(o =>
    {
        o.ChunkSize = 512;
        o.Overlap = 50;
    })
    .AddOllamaEmbedder(o =>
    {
        o.Endpoint = "http://localhost:11434";
        o.EmbeddingModel = "nomic-embed-text";
    })
    .AddPostgresVectorStore(o =>
    {
        o.ConnectionString = "Host=localhost;Database=rag;Username=postgres;Password=postgres";
        o.VectorDimension = 768;
    });
```

### 2. Ingest and query

Using a built-in document type:

```csharp
var sourceId = new Guid("a34a3c8c-9a31-45f0-b5f7-d83b4ad62d11"); // stable, never changes

// Ingest
await pipeline.IngestAsync(new PlainTextDocument
{
    Source = new Document.Origin(sourceId, "Invoice", "INV-001"),
    Text = invoiceText
});

// Query
var results = await pipeline.QueryAsync("your question");

foreach (var result in results)
    Console.WriteLine($"[{result.Score:F2}] {result.Chunk.Text}");
```

### 3. Replace a document

When a document changes, delete its old chunks before re-ingesting:

```csharp
await vectorStore.DeleteByDocumentAsync(doc.Source);
await pipeline.IngestAsync(updatedDoc);
```

## Chunking strategies

### Built-in chunkers

Both chunkers operate on `PlainTextDocument` and are registered for the same document type — choose one per registration.

**`AddPlainTextChunker`** — fixed character-size chunks with overlap:

```csharp
.AddPlainTextChunker(o =>
{
    o.ChunkSize = 512;          // max characters per chunk
    o.Overlap = 50;             // shared characters between consecutive chunks
    o.RespectWordBoundaries = true;  // avoid cutting mid-word (default: true)
    o.MinChunkLength = 20;      // drop trailing fragments shorter than this
})
```

**`AddSentenceChunker`** — accumulates sentences up to a character limit; paragraph breaks are always hard boundaries:

```csharp
.AddSentenceChunker(o =>
{
    o.MaxChunkSize = 512;   // max characters per chunk
    o.MinChunkLength = 20;  // drop short fragments
})
```

### Custom document types and chunkers

For document types with structure beyond plain text, subclass `Document` directly and provide your own `IChunker<T>`:

```csharp
// 1. Define your document type
public record InvoiceDocument : Document
{
    private static readonly Guid SourceId = new("a34a3c8c-9a31-45f0-b5f7-d83b4ad62d11");

    [SetsRequiredMembers]
    public InvoiceDocument(string text, string invoiceId)
    {
        Text = text;
        Source = new Document.Origin(SourceId, "Invoice", invoiceId);
    }

    public required string Text { get; init; }
}

// 2. Implement a chunker
public class InvoiceChunker : IChunker<InvoiceDocument>
{
    public async IAsyncEnumerable<Chunk> ChunkAsync(
        InvoiceDocument document,
        CancellationToken cancellationToken = default)
    {
        // your chunking logic
    }
}

// 3. Register
services.AddRagToolkit()
    .AddChunker<InvoiceDocument, InvoiceChunker>()
    ...
```

The dispatcher routes each document to its registered chunker automatically. If a document type has no registered chunker, an `InvalidOperationException` is thrown with the type name.

## Prerequisites

- .NET 9 SDK
- PostgreSQL with the `vector` extension installed (`CREATE EXTENSION IF NOT EXISTS vector`)
- Ollama running locally with an embedding model pulled (`ollama pull nomic-embed-text`)
- Docker (for integration tests)

## Core concepts

### Pipeline flow

```
Ingest:  Document → IChunker<T> → IEmbedder → IVectorStore
Query:   string   → IEmbedder → IRetriever → IReadOnlyList<SearchResult>
```

The `ChunkerDispatcher` (registered automatically by `AddRagToolkit`) routes each `Document` instance to the `IChunker<T>` registered for that document type, walking the inheritance chain if no exact match exists.

### Document identity

Every `Document` carries a `Source` property of type `Document.Origin`:

```csharp
public sealed record Origin(Guid SourceId, string DocumentType, string DocumentId)
```

| Field | Purpose |
|---|---|
| `SourceId` | Identifies the source system (one Guid per document class, stable forever) |
| `DocumentType` | Identifies the document category within that system (`"Invoice"`, `"Contract"`) |
| `DocumentId` | Identifies the specific document instance (`"INV-001"`) |

Origin is propagated automatically to every `Chunk` produced during ingestion and stored as dedicated columns in the vector store. This enables `DeleteByDocumentAsync` to atomically remove all chunks belonging to a specific document.

### Chunk enrichment

The pipeline automatically enriches each chunk before storage:

| Property | Set by | Value |
|---|---|---|
| `Chunk.Id` | `RagPipeline` | Random `Guid` |
| `Chunk.Embedding` | `RagPipeline` | Output of `IEmbedder` |
| `Chunk.Origin` | `IChunker<T>` | Copied from `Document.Source` |
| `Chunk.ChunkIndex` | `RagPipeline` | Zero-based position within the document |

### Similarity score

`SearchResult.Score` is cosine similarity in `[-1, 1]`:
- `1.0` — identical meaning
- `0.0` — unrelated (orthogonal)
- `-1.0` — opposite meaning

`RetrievalOptions.MinScore` defaults to `0.0` — results with score `<= 0` are excluded.

### Adding a new provider

Create a new project referencing only `IV.RagToolkit.Abstractions`, implement the relevant interface, and register via a `RagToolkitBuilder` extension:

```csharp
// IV.RagToolkit.Qdrant
public static RagToolkitBuilder AddQdrantVectorStore(
    this RagToolkitBuilder builder,
    Action<QdrantOptions> configure) { ... }
```

The consumer swaps one line in DI — no other code changes.

## Solution structure

```
src/
  IV.RagToolkit.Abstractions/
  IV.RagToolkit.Core/
  IV.RagToolkit.Ollama/
  IV.RagToolkit.Postgres/
tests/
  unit/                          ← no infrastructure required
  integration/                   ← Docker (Testcontainers)
  e2e/                           ← live Ollama + Postgres
automation/                      ← build and publish scripts
```

## Building

```bash
dotnet build IV.RagToolkit.sln
```

## Testing

```bash
# Unit tests — fast, no infrastructure
dotnet test IV.RagToolkit.Unit.slnf

# Integration tests — requires Docker
dotnet test IV.RagToolkit.Integration.slnf

# E2E tests — requires Ollama running at http://localhost:11434
dotnet test IV.RagToolkit.E2E.slnf
```

## Extending retrieval options

```csharp
var results = await pipeline.QueryAsync(
    "your question",
    new RetrievalOptions
    {
        TopK = 10,       // maximum results to return
        MinScore = 0.7f  // only return highly relevant results
    });
```
