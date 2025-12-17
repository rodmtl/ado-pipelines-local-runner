using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Core.Schema;

/// <summary>
/// Implementation of ISchemaManager.
/// Manages Azure DevOps pipeline schema definitions with local caching.
/// Implements Single Responsibility through separated validation and loading concerns.
/// </summary>
public class SchemaManager : ISchemaManager
{
    private readonly Dictionary<string, SchemaDefinition> _schemaCache = new();
    private readonly string _defaultSchemaVersion = "1.0.0";

    public SchemaManager()
    {
        InitializeDefaultSchemas();
    }

    /// <inheritdoc />
    public async Task<SchemaValidationResult> ValidateAsync(
        PipelineDocument document,
        string? schemaVersion = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ValidateInternal(document, schemaVersion), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SchemaDefinition> LoadSchemaAsync(string schemaSource, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadSchemaInternal(schemaSource), cancellationToken);
    }

    /// <inheritdoc />
    public string GetDefaultSchemaVersion()
    {
        return _defaultSchemaVersion;
    }

    /// <inheritdoc />
    public TypeSchema? GetTypeSchema(string typeName, string? schemaVersion = null)
    {
        var version = schemaVersion ?? _defaultSchemaVersion;
        if (_schemaCache.TryGetValue(version, out var schema))
        {
            return schema.Types.TryGetValue(typeName, out var typeSchema) ? typeSchema : null;
        }
        return null;
    }

    /// <summary>
    /// Validates a pipeline document against a specific schema version.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Schema validation orchestration.
    /// Delegates validation steps to focused helper methods.
    /// </remarks>
    private SchemaValidationResult ValidateInternal(PipelineDocument document, string? schemaVersion)
    {
        var version = schemaVersion ?? _defaultSchemaVersion;
        var errors = new List<ValidationError>();

        try
        {
            // Verify schema exists
            if (!_schemaCache.TryGetValue(version, out var _))
                return CreateSchemaNotFoundError(version, document);

            // Validate document structure
            ValidateDocumentStructure(document, errors);

            return new SchemaValidationResult
            {
                IsValid = errors.Count == 0,
                SchemaVersion = version,
                Errors = errors.ToArray()
            };
        }
        catch (Exception ex)
        {
            errors.Add(CreateValidationException(document, ex));
            return new SchemaValidationResult
            {
                IsValid = false,
                SchemaVersion = version,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Creates an error result when schema is not found.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Error creation for missing schema.
    /// </remarks>
    private SchemaValidationResult CreateSchemaNotFoundError(string version, PipelineDocument? document)
    {
        var error = new ValidationError
        {
            Code = "SCHEMA_NOT_FOUND",
            Message = $"Schema version '{version}' not found",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = document?.SourcePath ?? "<unknown>", Line = 1, Column = 1 }
        };
        return new SchemaValidationResult { IsValid = false, SchemaVersion = version, Errors = new[] { error } };
    }

    /// <summary>
    /// Validates the structure of a pipeline document.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Document structure validation.
    /// </remarks>
    private void ValidateDocumentStructure(PipelineDocument? document, List<ValidationError> errors)
    {
        if (document == null)
        {
            errors.Add(new ValidationError
            {
                Code = "SCHEMA_INVALID_DOCUMENT",
                Message = "Pipeline document is null",
                Severity = Severity.Error,
                Location = new SourceLocation { FilePath = "<unknown>", Line = 1, Column = 1 }
            });
            return;
        }

        ValidateTriggerDefinition(document, errors);
        ValidateWorkDefinition(document, errors);
    }

    /// <summary>
    /// Validates that a trigger is defined.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Trigger validation.
    /// </remarks>
    private void ValidateTriggerDefinition(PipelineDocument document, List<ValidationError> errors)
    {
        if (document.Trigger != null)
            return;

        errors.Add(new ValidationError
        {
            Code = "SCHEMA_MISSING_TRIGGER",
            Message = "Required property 'trigger' is missing",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = document.SourcePath ?? "<unknown>", Line = 1, Column = 1 },
            Suggestion = "Add a trigger section (e.g. 'trigger: none' or branches)."
        });
    }

    /// <summary>
    /// Validates that work is defined (stages, jobs, or steps).
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Work definition validation.
    /// </remarks>
    private void ValidateWorkDefinition(PipelineDocument document, List<ValidationError> errors)
    {
        var hasStages = document.Stages?.Count > 0;
        var hasJobs = document.Jobs?.Count > 0;
        var hasSteps = document.Steps?.Count > 0;

        if (hasStages || hasJobs || hasSteps)
            return;

        errors.Add(new ValidationError
        {
            Code = "SCHEMA_MISSING_WORK",
            Message = "Pipeline must define stages, jobs, or steps",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = document.SourcePath ?? "<unknown>", Line = 1, Column = 1 },
            Suggestion = "Add at least one stage, job, or step definition."
        });
    }

    /// <summary>
    /// Creates an error from an exception during validation.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Exception error creation.
    /// </remarks>
    private ValidationError CreateValidationException(PipelineDocument? document, Exception ex) =>
        new()
        {
            Code = "SCHEMA_VALIDATION_ERROR",
            Message = $"Error during schema validation: {ex.Message}",
            Severity = Severity.Error,
            Location = new SourceLocation { FilePath = document?.SourcePath ?? "<unknown>", Line = 1, Column = 1 }
        };

    /// <summary>
    /// Loads a schema definition from cache or creates a minimal one.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Schema loading.
    /// Phase 1 returns minimal schema; real implementation would load from file/web.
    /// </remarks>
    private SchemaDefinition LoadSchemaInternal(string schemaSource)
    {
        if (_schemaCache.TryGetValue(schemaSource, out var cached))
            return cached;

        return CreateMinimalSchema(schemaSource);
    }

    /// <summary>
    /// Creates a minimal schema definition for a given version.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Minimal schema creation.
    /// </remarks>
    private SchemaDefinition CreateMinimalSchema(string version) =>
        new()
        {
            Version = version,
            Schema = "https://raw.githubusercontent.com/microsoft/azure-pipelines-vscode/main/service-schema.json",
            Types = new Dictionary<string, TypeSchema>
            {
                { "Pipeline", new TypeSchema { Name = "Pipeline", Properties = new Dictionary<string, PropertySchema>().AsReadOnly() } }
            },
            RootType = "Pipeline"
        };

    /// <summary>
    /// Initializes default schema definitions for the supported versions.
    /// </summary>
    /// <remarks>
    /// Single Responsibility: Default schema initialization.
    /// </remarks>
    private void InitializeDefaultSchemas()
    {
        var defaultSchema = new SchemaDefinition
        {
            Version = _defaultSchemaVersion,
            Schema = "https://raw.githubusercontent.com/microsoft/azure-pipelines-vscode/main/service-schema.json",
            Types = new Dictionary<string, TypeSchema>
            {
                { "Pipeline", new TypeSchema { Name = "Pipeline", Properties = new Dictionary<string, PropertySchema>().AsReadOnly() } },
                { "Stage", new TypeSchema { Name = "Stage", Properties = new Dictionary<string, PropertySchema>().AsReadOnly() } },
                { "Job", new TypeSchema { Name = "Job", Properties = new Dictionary<string, PropertySchema>().AsReadOnly() } },
                { "Step", new TypeSchema { Name = "Step", Properties = new Dictionary<string, PropertySchema>().AsReadOnly() } }
            },
            RootType = "Pipeline",
            Metadata = new Dictionary<string, string>
            {
                { "description", "Azure DevOps Pipeline schema v1.0" }
            }
        };

        _schemaCache[_defaultSchemaVersion] = defaultSchema;
    }
}
