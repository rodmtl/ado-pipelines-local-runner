namespace AdoPipelinesLocalRunner.Contracts;

/// <summary>
/// Validates YAML pipeline syntax against Azure DevOps structural rules.
/// </summary>
public interface ISyntaxValidator
{
    /// <summary>
    /// Validates a parsed pipeline document against syntax rules.
    /// </summary>
    /// <param name="document">Parsed pipeline document to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<ValidationResult> ValidateAsync(PipelineDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a custom validation rule.
    /// </summary>
    /// <param name="rule">Validation rule to add</param>
    void AddRule(IValidationRule rule);

    /// <summary>
    /// Removes a validation rule by name.
    /// </summary>
    /// <param name="ruleName">Name of the rule to remove</param>
    /// <returns>True if rule was removed, false if not found</returns>
    bool RemoveRule(string ruleName);

    /// <summary>
    /// Gets all registered validation rules.
    /// </summary>
    /// <returns>Collection of validation rules</returns>
    IReadOnlyList<IValidationRule> GetRules();
}

/// <summary>
/// Defines a validation rule for pipeline syntax.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Unique name identifying this rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Severity level for violations of this rule.
    /// </summary>
    Severity Severity { get; }

    /// <summary>
    /// Description of what this rule validates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Validates a pipeline document node.
    /// </summary>
    /// <param name="node">The node to validate</param>
    /// <param name="context">Validation context</param>
    /// <returns>Collection of validation errors found</returns>
    IEnumerable<ValidationError> Validate(object node, ValidationContext context);
}

/// <summary>
/// Context provided during validation.
/// </summary>
public record ValidationContext
{
    /// <summary>
    /// The complete pipeline document being validated.
    /// </summary>
    public required PipelineDocument Document { get; init; }

    /// <summary>
    /// Source map for location tracking.
    /// </summary>
    public required ISourceMap SourceMap { get; init; }

    /// <summary>
    /// Current property path being validated.
    /// </summary>
    public required string CurrentPath { get; init; }

    /// <summary>
    /// Parent node in the hierarchy (null for root).
    /// </summary>
    public object? Parent { get; init; }
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Indicates whether validation passed (no errors).
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Collection of validation errors.
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }

    /// <summary>
    /// Collection of validation warnings.
    /// </summary>
    public required IReadOnlyList<ValidationError> Warnings { get; init; }

    /// <summary>
    /// Total count of issues found.
    /// </summary>
    public int TotalIssues => Errors.Count + Warnings.Count;
}

/// <summary>
/// Severity level for validation issues.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning that doesn't prevent execution.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error that prevents valid execution.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error requiring immediate attention.
    /// </summary>
    Critical = 3
}
