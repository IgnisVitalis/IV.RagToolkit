# CLAUDE.md

Instructions for Claude Code when working in this repository.

## Project

`IV.RAG` — a .NET 9 NuGet toolkit providing infrastructure and base classes for RAG (Retrieval-Augmented Generation) pipelines. Designed to be composable: consumers swap providers via DI without touching pipeline logic.

**Development stage:** pre-1.0, active development. Backward compatibility is not required — breaking changes to any layer including `Abstractions` are acceptable.

## Solution structure

```
IV.RAG.sln               ← open this in IDE
src/
  IV.RAG.Abstractions    ← interfaces and models only, no implementations
  IV.RAG.Core            ← pipeline orchestrators (RagPipeline, RetrievalPipeline, AnswerPipeline)
  IV.RAG.Ingestion       ← document types + chunkers (PlainTextDocument, FixedSizeChunker, SentenceChunker)
  IV.RAG.Ollama          ← IEmbedder + IGenerator backed by Ollama HTTP API
  IV.RAG.Postgres        ← IVectorStore + IRetriever backed by pgvector via Npgsql
  IV.RAG.Remote.Http     ← IRetrievalPipeline proxy — calls a remote retrieval server over HTTP
tests/
  unit/                  ← no infrastructure required, fast
  integration/           ← requires Docker (Postgres via Testcontainers, Ollama external)
automation/              ← scripts (build, pack, publish)
Directory.Build.props    ← shared: TargetFramework, Nullable, TreatWarningsAsErrors
Directory.Packages.props ← central NuGet version management
```

## Package taxonomy

| Layer | Package | Role |
|---|---|---|
| Abstractions | IV.RAG.Abstractions | interfaces + models |
| Orchestration | IV.RAG.Core | pipeline wiring |
| Ingestion | IV.RAG.Ingestion | document processing |
| Providers | IV.RAG.Ollama | embedder + generator |
| Providers | IV.RAG.Postgres | vector store + retriever |
| Providers | IV.RAG.Remote.Http | remote retrieval proxy |

## Dependency rule

`Abstractions` has no project references. All other packages reference only `Abstractions`. Nothing in `src/` references sibling packages. Consumers wire providers together at startup.

## Common commands

```bash
# Build
dotnet build IV.RAG.sln

# Unit tests (no infra needed)
dotnet test tests/unit/

# Integration tests (requires Docker)
dotnet test tests/integration/

# Pack a specific package
dotnet pack src/IV.RAG.Abstractions/ -c Release
```

## Conventions

- All packages share the version defined in `Directory.Build.props`
- Never add `Version` attributes to `<PackageReference>` — versions live in `Directory.Packages.props`
- `TreatWarningsAsErrors` is on; all public APIs need XML doc comments
- Test projects set `<IsTestProject>true</IsTestProject>` to opt out of doc generation
- No comments unless the WHY is non-obvious
- No commit unless explicitly asked

## Behavior rules

- If a request contains a question, discuss before modifying code
- Be a constructive skeptic on design decisions
- Prefer small, focused changes
- Breaking changes to `Abstractions` are acceptable — the project is pre-1.0
- Use existing patterns and naming conventions in the repo
