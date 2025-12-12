namespace AdoPipelinesLocalRunner.Contracts;

/// <summary>
/// Resolves and expands pipeline templates from various sources.
/// </summary>
public interface ITemplateResolver
{
    /// <summary>
    /// Resolves a template reference and returns its content.
    /// </summary>
    /// <param name="templateReference">Template reference (file path, URL, or repository reference)</param>
    /// <param name="context">Resolution context with base paths and parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved template content and metadata</returns>
    Task<TemplateResolutionResult> ResolveAsync(
        string templateReference,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands templates in a pipeline document recursively.
    /// </summary>
    /// <param name="document">Pipeline document containing template references</param>
    /// <param name="context">Resolution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Expanded pipeline document with all templates resolved</returns>
    Task<TemplateExpansionResult> ExpandAsync(
        PipelineDocument document,
        TemplateResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a template reference without resolving it.
    /// </summary>
    /// <param name="templateReference">Template reference to validate</param>
    /// <param name="context">Resolution context</param>
    /// <returns>True if reference is valid and resolvable</returns>
    Task<bool> ValidateReferenceAsync(
        string templateReference,
        TemplateResolutionContext context);
}

/// <summary>
/// Context for template resolution operations.
/// </summary>
public record TemplateResolutionContext
{
    /// <summary>
    /// Base directory for relative template paths.
    /// </summary>
    public required string BaseDirectory { get; init; }

    /// <summary>
    /// Parameters to pass to templates.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }

    /// <summary>
    /// Maximum depth for nested template resolution.
    /// </summary>
    public int MaxDepth { get; init; } = 10;

    /// <summary>
    /// Current resolution depth (for cycle detection).
    /// </summary>
    public int CurrentDepth { get; init; }

    /// <summary>
    /// Stack of resolved template paths (for cycle detection).
    /// </summary>
    public IReadOnlyList<string>? ResolutionStack { get; init; }

    /// <summary>
    /// Repository context for remote template resolution.
    /// </summary>
    public RepositoryContext? RepositoryContext { get; init; }
}

/// <summary>
/// Repository context for remote template resolution.
/// </summary>
public record RepositoryContext
{
    /// <summary>
    /// Repository URL or identifier.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Branch, tag, or commit reference.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Authentication token if required.
    /// </summary>
    public string? AuthToken { get; init; }
}

/// <summary>
/// Result of a template resolution operation.
/// </summary>
public record TemplateResolutionResult
{
    /// <summary>
    /// Indicates whether resolution succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Resolved template content (YAML string).
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Source location of the template.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Errors encountered during resolution.
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }

    /// <summary>
    /// Metadata about the template.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Result of template expansion operation.
/// </summary>
public record TemplateExpansionResult
{
    /// <summary>
    /// Indicates whether expansion succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Fully expanded pipeline document.
    /// </summary>
    public PipelineDocument? ExpandedDocument { get; init; }

    /// <summary>
    /// Collection of all resolved templates.
    /// </summary>
    public required IReadOnlyList<ResolvedTemplate> ResolvedTemplates { get; init; }

    /// <summary>
    /// Errors encountered during expansion.
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }
}

/// <summary>
/// Information about a resolved template.
/// </summary>
public record ResolvedTemplate
{
    /// <summary>
    /// Original template reference.
    /// </summary>
    public required string Reference { get; init; }

    /// <summary>
    /// Resolved source path or URL.
    /// </summary>
    public required string ResolvedSource { get; init; }

    /// <summary>
    /// Parameters used for this template.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }

    /// <summary>
    /// Resolution depth level.
    /// </summary>
    public required int Depth { get; init; }
}
