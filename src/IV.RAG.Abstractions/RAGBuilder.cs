using Microsoft.Extensions.DependencyInjection;

namespace IV.RAG;

/// <summary>
/// Fluent builder returned by <c>AddRagToolkit()</c>.
/// Provider packages extend this with their own registration methods.
/// </summary>
public sealed class RAGBuilder(IServiceCollection services)
{
    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; } = services;
}
