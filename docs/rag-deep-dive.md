# RAG deep-dive questions

Questions worth understanding thoroughly before building production RAG systems.

---

## Chunking

1. How does chunk size affect retrieval quality? What are the trade-offs between small chunks (precise but context-poor) and large chunks (context-rich but diluted)?
2. What is the "lost in the middle" problem in chunking, and how does overlap address it — and where does overlap fail?
3. Should you chunk by characters, tokens, sentences, or semantic paragraphs? When does each strategy win?
4. What is the "parent-child" chunking pattern? When is it better to embed small chunks but retrieve their larger parent for generation?
5. How do you handle documents with structure (headings, tables, code blocks) that fixed-size chunking breaks apart?

## Embedding models

6. What makes an embedding model good for RAG specifically, as opposed to classification or clustering?
7. How does embedding dimensionality (768 vs 1536 vs 3072) affect retrieval quality and storage/latency cost?
8. What is matryoshka representation learning (MRL) and why does it allow truncating embeddings without catastrophic quality loss?
9. What happens when you mix embeddings from two different models in the same vector store? Why is model versioning a hard operational problem?
10. How do you evaluate embedding model quality for a specific domain before committing to it?

## Similarity search

11. Why is cosine similarity preferred over Euclidean distance for semantic search? When does the choice matter?
12. What is the difference between exact KNN and approximate nearest neighbor (ANN) search? At what scale does the switch become necessary?
13. What is the difference between IVFFlat and HNSW indexes in pgvector? What are the build-time vs query-time trade-offs?
14. What is "ef_search" in HNSW and how does it control the recall/speed trade-off?

## Metadata filtering and access control

15. What is the difference between pre-filtering and post-filtering in vector search? How does each affect TopK semantics?
16. With pgvector, metadata filters are applied post-ANN-scan — what does this mean for result count guarantees, and how does it differ from Qdrant or Weaviate's filtered ANN?
17. What metadata schema design choices affect filter expressiveness and index efficiency? (e.g. flat vs nested, string vs numeric values)
18. How do metadata filters implement row-level access control in RAG? What are the failure modes if the filter is accidentally omitted?
19. What is the trade-off between per-tenant tables and a shared table with a tenant metadata column?

## Retrieval quality

20. What is the difference between retrieval precision and recall in RAG? Which matters more, and why does it depend on the use case?
21. What is RAGAS? What metrics does it measure — faithfulness, answer relevancy, context precision, context recall — and what does each catch?
22. When does vector search fail? What types of queries (exact IDs, negations, counts, comparisons) does semantic similarity handle poorly?
23. What is re-ranking? When is it worth the extra latency and cost to run a cross-encoder after first-stage retrieval?

## Hybrid search

24. What is BM25 and why does combining it with dense vector search improve results on keyword-heavy queries?
25. What is Reciprocal Rank Fusion (RRF)? How does it merge rankings from two retrieval systems without requiring score normalisation?
26. What is SPLADE and how does it differ from BM25 + dense hybrid approaches?

## Advanced retrieval

27. What is HyDE (Hypothetical Document Embeddings)? Why does embedding a generated answer sometimes retrieve better than embedding the question?
28. What is query expansion? When is it worth the extra LLM call, and what are the risks (hallucinated expansions)?
29. What is multi-vector retrieval (ColBERT / late interaction)? How does it differ from single-vector retrieval and when does it win?
30. What is the "step-back prompting" technique and how does it improve retrieval on abstract or indirect questions?

## Generation quality

31. What is the context window packing problem? How do you decide which chunks to include in the prompt when retrieval returns more than fits?
32. Does the order of chunks in the prompt affect generation quality? What is the "lost in the middle" LLM effect?
33. What is "faithfulness" in RAG generation? How do you detect when the model generates content not grounded in the retrieved chunks?
34. When should RAG return "I don't know" instead of generating an answer? How do you implement a confidence threshold?

## Production and operations

35. How do you profile the latency breakdown of a RAG pipeline (embedding, retrieval, generation)? Where are the typical bottlenecks?
36. How do you handle embedding model upgrades without downtime? What re-ingestion strategies exist?
37. How do you detect retrieval drift over time — cases where the retrieval quality degrades as the document corpus grows or changes?
38. What is the difference between online (real-time) and offline (batch) ingestion? When does each apply?
