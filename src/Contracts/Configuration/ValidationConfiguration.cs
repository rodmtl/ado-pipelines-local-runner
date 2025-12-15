namespace AdoPipelinesLocalRunner.Contracts.Configuration;

/// <summary>
/// Configuration for pipeline validation behavior.
/// </summary>
public record ValidationConfiguration
{
    /// <summary>
    /// Whether to enable syntax validation.
    /// </summary>
    public bool EnableSyntaxValidation { get; init; } = true;

    /// <summary>
    /// Whether to enable schema validation.
    /// </summary>
    public bool EnableSchemaValidation { get; init; } = true;

    /// <summary>
    /// Whether to enable template resolution.
    /// </summary>
    public bool EnableTemplateResolution { get; init; } = true;

    /// <summary>
    /// Whether to enable variable processing.
    /// </summary>
    public bool EnableVariableProcessing { get; init; } = true;

    /// <summary>
    /// Schema version to validate against.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Whether to treat warnings as errors.
    /// </summary>
    public bool TreatWarningsAsErrors { get; init; } = false;

    /// <summary>
    /// Maximum template nesting depth.
    /// </summary>
    public int MaxTemplateDepth { get; init; } = 10;

    /// <summary>
    /// Timeout for validation operations in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Custom validation rules to enable.
    /// </summary>
    public IReadOnlyList<string>? EnabledRules { get; init; }

    /// <summary>
    /// Validation rules to disable.
    /// </summary>
    public IReadOnlyList<string>? DisabledRules { get; init; }

    /// <summary>
    /// Path patterns to exclude from validation.
    /// </summary>
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
}

/// <summary>
/// Configuration for template resolution.
/// </summary>
public record TemplateConfiguration
{
    /// <summary>
    /// Base directories to search for templates.
    /// </summary>
    public required IReadOnlyList<string> BasePaths { get; init; }

    /// <summary>
    /// Whether to allow remote template URLs.
    /// </summary>
    public bool AllowRemoteTemplates { get; init; } = true;

    /// <summary>
    /// Whether to cache resolved templates.
    /// </summary>
    public bool EnableCache { get; init; } = true;

    /// <summary>
    /// Cache directory for remote templates.
    /// </summary>
    public string? CacheDirectory { get; init; }

    /// <summary>
    /// Repository configurations for remote templates.
    /// </summary>
    public IReadOnlyList<RepositoryConfiguration>? Repositories { get; init; }

    /// <summary>
    /// Timeout for template downloads in seconds.
    /// </summary>
    public int DownloadTimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Configuration for a specific repository.
/// </summary>
public record RepositoryConfiguration
{
    /// <summary>
    /// Repository identifier or URL pattern.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Authentication token for private repositories.
    /// </summary>
    public string? AuthToken { get; init; }

    /// <summary>
    /// Default reference (branch/tag) to use.
    /// </summary>
    public string? DefaultReference { get; init; }

    /// <summary>
    /// Base path within the repository for templates.
    /// </summary>
    public string? BasePath { get; init; }
}

/// <summary>
/// Configuration for variable processing.
/// </summary>
public record VariableConfiguration
{
    /// <summary>
    /// Paths to variable files to load.
    /// </summary>
    public IReadOnlyList<string>? VariableFiles { get; init; }

    /// <summary>
    /// Variable groups to mock.
    /// </summary>
    public IReadOnlyDictionary<string, VariableGroupConfiguration>? MockVariableGroups { get; init; }

    /// <summary>
    /// Whether to fail on undefined variables.
    /// </summary>
    public bool FailOnUndefined { get; init; } = true;

    /// <summary>
    /// Whether to preserve secret variable values in output.
    /// </summary>
    public bool PreserveSecrets { get; init; } = true;

    /// <summary>
    /// System variables to override.
    /// </summary>
    public IReadOnlyDictionary<string, object>? SystemVariableOverrides { get; init; }
}

/// <summary>
/// Configuration for a mock variable group.
/// </summary>
public record VariableGroupConfiguration
{
    /// <summary>
    /// Variable group name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Variables in this group.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Variables { get; init; }

    /// <summary>
    /// Whether this group contains secrets.
    /// </summary>
    public bool IsSecret { get; init; } = false;

    /// <summary>
    /// Description of the variable group.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Configuration for schema management.
/// </summary>
public record SchemaConfiguration
{
    /// <summary>
    /// Path to custom schema file.
    /// </summary>
    public string? CustomSchemaPath { get; init; }

    /// <summary>
    /// URL to fetch schema from.
    /// </summary>
    public string? SchemaUrl { get; init; }

    /// <summary>
    /// Whether to use embedded schema.
    /// </summary>
    public bool UseEmbeddedSchema { get; init; } = true;

    /// <summary>
    /// Whether to validate unknown properties.
    /// </summary>
    public bool StrictMode { get; init; } = false;

    /// <summary>
    /// Custom type mappings.
    /// </summary>
    public IReadOnlyDictionary<string, string>? TypeMappings { get; init; }
}

/// <summary>
/// Complete application configuration.
/// </summary>
public record ApplicationConfiguration
{
    /// <summary>
    /// Validation configuration.
    /// </summary>
    public required ValidationConfiguration Validation { get; init; }

    /// <summary>
    /// Template configuration.
    /// </summary>
    public required TemplateConfiguration Template { get; init; }

    /// <summary>
    /// Variable configuration.
    /// </summary>
    public required VariableConfiguration Variable { get; init; }

    /// <summary>
    /// Schema configuration.
    /// </summary>
    public required SchemaConfiguration Schema { get; init; }

    /// <summary>
    /// Logging configuration.
    /// </summary>
    public LoggingConfiguration? Logging { get; init; }

    /// <summary>
    /// Output configuration.
    /// </summary>
    public OutputConfiguration? Output { get; init; }
}

/// <summary>
/// Configuration for logging behavior.
/// </summary>
public record LoggingConfiguration
{
    /// <summary>
    /// Minimum log level.
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Whether to log to console.
    /// </summary>
    public bool LogToConsole { get; init; } = true;

    /// <summary>
    /// Whether to log to file.
    /// </summary>
    public bool LogToFile { get; init; } = false;

    /// <summary>
    /// Log file path.
    /// </summary>
    public string? LogFilePath { get; init; }

    /// <summary>
    /// Whether to include timestamps.
    /// </summary>
    public bool IncludeTimestamps { get; init; } = true;
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

/// <summary>
/// Configuration for output formatting.
/// </summary>
public record OutputConfiguration
{
    /// <summary>
    /// Default output format.
    /// </summary>
    public OutputFormat DefaultFormat { get; init; } = OutputFormat.Text;

    /// <summary>
    /// Whether to colorize console output.
    /// </summary>
    public bool UseColors { get; init; } = true;

    /// <summary>
    /// Whether to include detailed information.
    /// </summary>
    public bool Verbose { get; init; } = false;

    /// <summary>
    /// Whether to show progress indicators.
    /// </summary>
    public bool ShowProgress { get; init; } = true;

    /// <summary>
    /// Maximum width for formatted output.
    /// </summary>
    public int MaxWidth { get; init; } = 120;
}

/// <summary>
/// Output format options for validation results.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Plain text output.
    /// </summary>
    Text = 0,

    /// <summary>
    /// JSON format output.
    /// </summary>
    Json = 1,

    /// <summary>
    /// SARIF (Static Analysis Results Interchange Format) output.
    /// </summary>
    Sarif = 2,

    /// <summary>
    /// Markdown format output.
    /// </summary>
    Markdown = 3
}
