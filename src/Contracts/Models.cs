namespace AdoPipelinesLocalRunner.Contracts;

/// <summary>
/// Represents a parsed Azure DevOps pipeline document.
/// </summary>
public record PipelineDocument
{
    /// <summary>
    /// Pipeline name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Trigger configuration.
    /// </summary>
    public object? Trigger { get; init; }

    /// <summary>
    /// Pipeline variables.
    /// </summary>
    public IReadOnlyList<object>? Variables { get; init; }

    /// <summary>
    /// Pipeline parameters.
    /// </summary>
    public IReadOnlyList<object>? Parameters { get; init; }

    /// <summary>
    /// Pipeline stages.
    /// </summary>
    public IReadOnlyList<object>? Stages { get; init; }

    /// <summary>
    /// Pipeline jobs (when stages are not used).
    /// </summary>
    public IReadOnlyList<object>? Jobs { get; init; }

    /// <summary>
    /// Pipeline steps (for simple pipelines).
    /// </summary>
    public IReadOnlyList<object>? Steps { get; init; }

    /// <summary>
    /// Resource definitions (repositories, containers, pipelines).
    /// </summary>
    public object? Resources { get; init; }

    /// <summary>
    /// Pipeline pool specification.
    /// </summary>
    public object? Pool { get; init; }

    /// <summary>
    /// Raw YAML content (for debugging).
    /// </summary>
    public string? RawContent { get; init; }

    /// <summary>
    /// Source file path.
    /// </summary>
    public string? SourcePath { get; init; }
}

/// <summary>
/// Represents a parsing or validation error.
/// </summary>
public record ParseError
{
    /// <summary>
    /// Error code identifier.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Error severity.
    /// </summary>
    public required Severity Severity { get; init; }

    /// <summary>
    /// Source location of the error.
    /// </summary>
    public SourceLocation? Location { get; init; }

    /// <summary>
    /// Related locations for additional context.
    /// </summary>
    public IReadOnlyList<SourceLocation>? RelatedLocations { get; init; }

    /// <summary>
    /// Suggested fixes or remediation steps.
    /// </summary>
    public string? Suggestion { get; init; }
}

/// <summary>
/// Represents a validation error (extends ParseError for clarity).
/// </summary>
public record ValidationError : ParseError;
