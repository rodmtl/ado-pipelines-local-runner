namespace AdoPipelinesLocalRunner.Contracts.Errors;

/// <summary>
/// Base exception for all pipeline validation errors.
/// </summary>
public abstract class PipelineException : Exception
{
    /// <summary>
    /// Error code for categorization.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Severity level of the error.
    /// </summary>
    public Severity Severity { get; }

    /// <summary>
    /// Source location where the error occurred.
    /// </summary>
    public SourceLocation? Location { get; }

    protected PipelineException(
        string errorCode,
        string message,
        Severity severity = Severity.Error,
        SourceLocation? location = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Severity = severity;
        Location = location;
    }
}

/// <summary>
/// Exception thrown when YAML parsing fails.
/// </summary>
public class YamlParseException : PipelineException
{
    public YamlParseException(
        string message,
        SourceLocation? location = null,
        Exception? innerException = null)
        : base("YAML_PARSE_ERROR", message, Severity.Error, location, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when syntax validation fails.
/// </summary>
public class SyntaxValidationException : PipelineException
{
    public ValidationResult ValidationResult { get; }

    public SyntaxValidationException(
        string message,
        ValidationResult validationResult,
        SourceLocation? location = null)
        : base("SYNTAX_VALIDATION_ERROR", message, Severity.Error, location)
    {
        ValidationResult = validationResult;
    }
}

/// <summary>
/// Exception thrown when schema validation fails.
/// </summary>
public class SchemaValidationException : PipelineException
{
    public SchemaValidationResult ValidationResult { get; }

    public SchemaValidationException(
        string message,
        SchemaValidationResult validationResult,
        SourceLocation? location = null)
        : base("SCHEMA_VALIDATION_ERROR", message, Severity.Error, location)
    {
        ValidationResult = validationResult;
    }
}

/// <summary>
/// Exception thrown when template resolution fails.
/// </summary>
public class TemplateResolutionException : PipelineException
{
    /// <summary>
    /// Template reference that failed to resolve.
    /// </summary>
    public string TemplateReference { get; }

    public TemplateResolutionException(
        string templateReference,
        string message,
        SourceLocation? location = null,
        Exception? innerException = null)
        : base("TEMPLATE_RESOLUTION_ERROR", message, Severity.Error, location, innerException)
    {
        TemplateReference = templateReference;
    }
}

/// <summary>
/// Exception thrown when a circular template dependency is detected.
/// </summary>
public class CircularTemplateException : TemplateResolutionException
{
    /// <summary>
    /// Chain of template references forming the circular dependency.
    /// </summary>
    public IReadOnlyList<string> DependencyChain { get; }

    public CircularTemplateException(
        IReadOnlyList<string> dependencyChain,
        SourceLocation? location = null)
        : base(
            dependencyChain.LastOrDefault() ?? "unknown",
            $"Circular template dependency detected: {string.Join(" -> ", dependencyChain)}",
            location)
    {
        DependencyChain = dependencyChain;
    }
}

/// <summary>
/// Exception thrown when template depth exceeds maximum allowed.
/// </summary>
public class TemplateDepthExceededException : TemplateResolutionException
{
    /// <summary>
    /// Maximum allowed depth.
    /// </summary>
    public int MaxDepth { get; }

    /// <summary>
    /// Actual depth reached.
    /// </summary>
    public int ActualDepth { get; }

    public TemplateDepthExceededException(
        string templateReference,
        int maxDepth,
        int actualDepth,
        SourceLocation? location = null)
        : base(
            templateReference,
            $"Template nesting depth ({actualDepth}) exceeds maximum allowed ({maxDepth})",
            location)
    {
        MaxDepth = maxDepth;
        ActualDepth = actualDepth;
    }
}

/// <summary>
/// Exception thrown when variable processing fails.
/// </summary>
public class VariableProcessingException : PipelineException
{
    /// <summary>
    /// Variable name or expression that failed.
    /// </summary>
    public string VariableExpression { get; }

    public VariableProcessingException(
        string variableExpression,
        string message,
        SourceLocation? location = null,
        Exception? innerException = null)
        : base("VARIABLE_PROCESSING_ERROR", message, Severity.Error, location, innerException)
    {
        VariableExpression = variableExpression;
    }
}

/// <summary>
/// Exception thrown when a required variable is not defined.
/// </summary>
public class UndefinedVariableException : VariableProcessingException
{
    public UndefinedVariableException(
        string variableName,
        SourceLocation? location = null)
        : base(
            variableName,
            $"Variable '{variableName}' is not defined",
            location)
    {
    }
}

/// <summary>
/// Exception thrown when file or resource operations fail.
/// </summary>
public class ResourceException : PipelineException
{
    /// <summary>
    /// Resource path or identifier.
    /// </summary>
    public string ResourcePath { get; }

    public ResourceException(
        string resourcePath,
        string message,
        Exception? innerException = null)
        : base("RESOURCE_ERROR", message, Severity.Error, null, innerException)
    {
        ResourcePath = resourcePath;
    }
}

/// <summary>
/// Exception thrown when a configuration error is detected.
/// </summary>
public class ConfigurationException : PipelineException
{
    /// <summary>
    /// Configuration key or section that caused the error.
    /// </summary>
    public string ConfigurationKey { get; }

    public ConfigurationException(
        string configurationKey,
        string message,
        Exception? innerException = null)
        : base("CONFIGURATION_ERROR", message, Severity.Error, null, innerException)
    {
        ConfigurationKey = configurationKey;
    }
}

/// <summary>
/// Exception thrown when a pipeline definition is invalid.
/// </summary>
public class InvalidPipelineException : PipelineException
{
    /// <summary>
    /// Collection of validation errors that make the pipeline invalid.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public InvalidPipelineException(
        string message,
        IReadOnlyList<ValidationError> validationErrors,
        SourceLocation? location = null)
        : base("INVALID_PIPELINE", message, Severity.Error, location)
    {
        ValidationErrors = validationErrors;
    }
}
