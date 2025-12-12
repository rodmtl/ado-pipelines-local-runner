namespace AdoPipelinesLocalRunner.Contracts.Commands;

/// <summary>
/// Request for pipeline validation command.
/// </summary>
public record ValidateRequest
{
    /// <summary>
    /// Path to the pipeline YAML file or directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Whether to validate templates.
    /// </summary>
    public bool ValidateTemplates { get; init; } = true;

    /// <summary>
    /// Whether to validate variables.
    /// </summary>
    public bool ValidateVariables { get; init; } = true;

    /// <summary>
    /// Whether to validate against schema.
    /// </summary>
    public bool ValidateSchema { get; init; } = true;

    /// <summary>
    /// Specific schema version to validate against.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Additional variable files to load.
    /// </summary>
    public IReadOnlyList<string>? VariableFiles { get; init; }

    /// <summary>
    /// Variable groups to mock.
    /// </summary>
    public IReadOnlyDictionary<string, VariableGroup>? MockVariableGroups { get; init; }

    /// <summary>
    /// Base directory for template resolution.
    /// </summary>
    public string? BaseDirectory { get; init; }

    /// <summary>
    /// Output format (json, text, detailed).
    /// </summary>
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Text;

    /// <summary>
    /// Whether to include warnings in output.
    /// </summary>
    public bool IncludeWarnings { get; init; } = true;

    /// <summary>
    /// Whether to fail on warnings.
    /// </summary>
    public bool FailOnWarnings { get; init; } = false;

    /// <summary>
    /// Maximum validation depth for templates.
    /// </summary>
    public int MaxTemplateDepth { get; init; } = 10;
}

/// <summary>
/// Response from pipeline validation command.
/// </summary>
public record ValidateResponse
{
    /// <summary>
    /// Overall validation status.
    /// </summary>
    public required ValidationStatus Status { get; init; }

    /// <summary>
    /// Summary of validation results.
    /// </summary>
    public required ValidationSummary Summary { get; init; }

    /// <summary>
    /// Detailed validation results by category.
    /// </summary>
    public required ValidationDetails Details { get; init; }

    /// <summary>
    /// Processing metrics.
    /// </summary>
    public required ProcessingMetrics Metrics { get; init; }

    /// <summary>
    /// Validated pipeline document (if successful).
    /// </summary>
    public PipelineDocument? ValidatedDocument { get; init; }
}

/// <summary>
/// Validation status enumeration.
/// </summary>
public enum ValidationStatus
{
    /// <summary>
    /// Validation succeeded with no issues.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Validation succeeded with warnings.
    /// </summary>
    SuccessWithWarnings = 1,

    /// <summary>
    /// Validation failed with errors.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Validation could not complete due to critical error.
    /// </summary>
    Error = 3
}

/// <summary>
/// Summary of validation results.
/// </summary>
public record ValidationSummary
{
    /// <summary>
    /// Total number of files validated.
    /// </summary>
    public required int FilesValidated { get; init; }

    /// <summary>
    /// Total number of errors found.
    /// </summary>
    public required int ErrorCount { get; init; }

    /// <summary>
    /// Total number of warnings found.
    /// </summary>
    public required int WarningCount { get; init; }

    /// <summary>
    /// Total number of information messages.
    /// </summary>
    public required int InfoCount { get; init; }

    /// <summary>
    /// Total number of templates resolved.
    /// </summary>
    public int TemplatesResolved { get; init; }

    /// <summary>
    /// Total number of variables resolved.
    /// </summary>
    public int VariablesResolved { get; init; }
}

/// <summary>
/// Detailed validation results by category.
/// </summary>
public record ValidationDetails
{
    /// <summary>
    /// Syntax validation results.
    /// </summary>
    public ValidationResult? SyntaxValidation { get; init; }

    /// <summary>
    /// Schema validation results.
    /// </summary>
    public SchemaValidationResult? SchemaValidation { get; init; }

    /// <summary>
    /// Template resolution results.
    /// </summary>
    public TemplateExpansionResult? TemplateResolution { get; init; }

    /// <summary>
    /// Variable processing results.
    /// </summary>
    public VariableProcessingResult? VariableProcessing { get; init; }

    /// <summary>
    /// All validation errors across categories.
    /// </summary>
    public required IReadOnlyList<ValidationError> AllErrors { get; init; }

    /// <summary>
    /// All validation warnings across categories.
    /// </summary>
    public required IReadOnlyList<ValidationError> AllWarnings { get; init; }
}

/// <summary>
/// Processing metrics for validation operation.
/// </summary>
public record ProcessingMetrics
{
    /// <summary>
    /// Total processing time in milliseconds.
    /// </summary>
    public required long TotalTimeMs { get; init; }

    /// <summary>
    /// Time spent parsing YAML in milliseconds.
    /// </summary>
    public long ParsingTimeMs { get; init; }

    /// <summary>
    /// Time spent on syntax validation in milliseconds.
    /// </summary>
    public long SyntaxValidationTimeMs { get; init; }

    /// <summary>
    /// Time spent on schema validation in milliseconds.
    /// </summary>
    public long SchemaValidationTimeMs { get; init; }

    /// <summary>
    /// Time spent resolving templates in milliseconds.
    /// </summary>
    public long TemplateResolutionTimeMs { get; init; }

    /// <summary>
    /// Time spent processing variables in milliseconds.
    /// </summary>
    public long VariableProcessingTimeMs { get; init; }

    /// <summary>
    /// Timestamp when validation started.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Timestamp when validation completed.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }
}

/// <summary>
/// Output format for validation results.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Human-readable text format.
    /// </summary>
    Text = 0,

    /// <summary>
    /// JSON format for programmatic consumption.
    /// </summary>
    Json = 1,

    /// <summary>
    /// Detailed text format with all information.
    /// </summary>
    Detailed = 2,

    /// <summary>
    /// Minimal output (errors only).
    /// </summary>
    Minimal = 3
}
