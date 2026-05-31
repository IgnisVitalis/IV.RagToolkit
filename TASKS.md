# Tasks

Backlog ordered by priority. Complete items are removed.

## High priority

- [ ] **Delete-before-insert in `IngestAsync`**
  Call `DeleteByDocumentAsync(document.Source)` before embedding and upserting chunks.
  Makes re-ingestion idempotent: stale chunks from shorter or re-chunked documents are
  removed automatically. Current gap: re-ingesting a changed document leaves old chunks
  in the store.

- [ ] **`IGenerator` interface**
  Takes a query and a list of retrieved chunks, returns a generated answer string.
  Add `AnswerAsync` to `IRagPipeline` as the full RAG loop: retrieve + generate.
  Add `OllamaGenerator` backed by `/api/chat` endpoint.

- [ ] **Atomic document replacement**
  The delete-before-insert approach has a brief window where a concurrent query sees
  zero chunks for a document. Investigate wrapping delete + upsert in a single DB
  transaction. Requires either exposing transaction support on `IVectorStore` or
  handling it internally in `PostgresVectorStore`.

- [ ] **Metadata filtering in retrieval**
  Extend `RetrievalOptions` with a `MetadataFilter` property.
  Filter chunks by stored metadata values during similarity search.

## Medium priority

- [ ] **Hybrid search (vector + lexical/BM25)**
  Add `ILexicalRetriever` interface alongside `IRetriever`.
  Add `HybridRetriever` in Core that fuses both rankings via Reciprocal Rank Fusion (RRF).
  Add `PostgresLexicalRetriever` backed by `tsvector/tsquery`.

- [ ] **Semantic query cache**
  Add `IQueryCache` interface. Cache query embeddings and their results.
  Add `CachedRagPipeline` decorator in Core (transparent to consumers).
  Add in-memory implementation in Core.
  Add Postgres implementation in the Postgres package.
  Configurable similarity threshold (default 0.95) and TTL.

- [ ] **Embedding model versioning**
  Expose model identity (`provider`, `name`, `version`, `dimensions`) on `IEmbedder`.
  Store model metadata alongside each vector table.
  Detect model mismatch on startup and throw a clear error.
  Add `ClearAsync()` and `GetSourceIdsAsync()` to `IVectorStore` to support re-ingestion.

- [ ] **Multi-table / keyed DI support**
  Allow registering multiple `IVectorStore` instances for different domains.
  Each registration points to a different table with its own model config.

## Lower priority

- [ ] **`.editorconfig`**
  Standardise line endings and formatting across the solution.
