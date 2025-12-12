namespace AdoPipelinesLocalRunner.Contracts;

/// <summary>
/// Processes and resolves pipeline variables and expressions.
/// </summary>
public interface IVariableProcessor
{
    /// <summary>
    /// Processes all variables in a pipeline document.
    /// </summary>
    /// <param name="document">Pipeline document to process</param>
    /// <param name="context">Variable processing context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with processed document and resolved variables</returns>
    Task<VariableProcessingResult> ProcessAsync(
        PipelineDocument document,
        VariableContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a single variable expression.
    /// </summary>
    /// <param name="expression">Variable expression (e.g., "$(variableName)" or "${{ variables.name }}")</param>
    /// <param name="context">Variable context</param>
    /// <returns>Resolved value or original expression if not resolvable</returns>
    string ResolveExpression(string expression, VariableContext context);

    /// <summary>
    /// Expands variables in a text string.
    /// </summary>
    /// <param name="text">Text containing variable references</param>
    /// <param name="context">Variable context</param>
    /// <returns>Text with variables expanded</returns>
    string ExpandVariables(string text, VariableContext context);

    /// <summary>
    /// Validates variable definitions and references.
    /// </summary>
    /// <param name="document">Pipeline document to validate</param>
    /// <param name="context">Variable context</param>
    /// <returns>Validation result for variables</returns>
    ValidationResult ValidateVariables(PipelineDocument document, VariableContext context);
}

/// <summary>
/// Context for variable processing operations.
/// </summary>
public record VariableContext
{
    /// <summary>
    /// Predefined system variables.
    /// </summary>
    public required IReadOnlyDictionary<string, object> SystemVariables { get; init; }

    /// <summary>
    /// User-defined variables from the pipeline.
    /// </summary>
    public IReadOnlyDictionary<string, object>? PipelineVariables { get; init; }

    /// <summary>
    /// Variables from variable groups.
    /// </summary>
    public IReadOnlyDictionary<string, VariableGroup>? VariableGroups { get; init; }

    /// <summary>
    /// Environment-specific variables.
    /// </summary>
    public IReadOnlyDictionary<string, object>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Runtime parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }

    /// <summary>
    /// Variable scope (stage, job, step level).
    /// </summary>
    public VariableScope Scope { get; init; } = VariableScope.Pipeline;

    /// <summary>
    /// Whether to fail on unresolved variables.
    /// </summary>
    public bool FailOnUnresolved { get; init; } = true;
}

/// <summary>
/// Represents a variable group.
/// </summary>
public record VariableGroup
{
    /// <summary>
    /// Variable group name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Variables in this group.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Variables { get; init; }

    /// <summary>
    /// Whether this group contains secrets.
    /// </summary>
    public bool IsSecret { get; init; }

    /// <summary>
    /// Description of the variable group.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Variable scope levels.
/// </summary>
public enum VariableScope
{
    /// <summary>
    /// Pipeline-level scope.
    /// </summary>
    Pipeline = 0,

    /// <summary>
    /// Stage-level scope.
    /// </summary>
    Stage = 1,

    /// <summary>
    /// Job-level scope.
    /// </summary>
    Job = 2,

    /// <summary>
    /// Step-level scope.
    /// </summary>
    Step = 3
}

/// <summary>
/// Result of variable processing operation.
/// </summary>
public record VariableProcessingResult
{
    /// <summary>
    /// Indicates whether processing succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Processed pipeline document with resolved variables.
    /// </summary>
    public PipelineDocument? ProcessedDocument { get; init; }

    /// <summary>
    /// All resolved variables with their final values.
    /// </summary>
    public required IReadOnlyDictionary<string, ResolvedVariable> ResolvedVariables { get; init; }

    /// <summary>
    /// Unresolved variable references.
    /// </summary>
    public IReadOnlyList<string>? UnresolvedReferences { get; init; }

    /// <summary>
    /// Errors encountered during processing.
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }
}

/// <summary>
/// Information about a resolved variable.
/// </summary>
public record ResolvedVariable
{
    /// <summary>
    /// Variable name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Resolved value.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// Source of the variable (system, pipeline, group, etc.).
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Variable scope.
    /// </summary>
    public required VariableScope Scope { get; init; }

    /// <summary>
    /// Whether this is a secret variable.
    /// </summary>
    public bool IsSecret { get; init; }
}
