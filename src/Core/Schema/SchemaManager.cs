using AdoPipelinesLocalRunner.Contracts;

namespace AdoPipelinesLocalRunner.Core.Schema;

/// <summary>
/// Implementation of ISchemaManager.
/// Manages Azure DevOps pipeline schema definitions with local caching.
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

    private SchemaValidationResult ValidateInternal(PipelineDocument document, string? schemaVersion)
    {
        var version = schemaVersion ?? _defaultSchemaVersion;
        var errors = new List<ValidationError>();

        try
        {
            if (!_schemaCache.TryGetValue(version, out var schema))
            {
                errors.Add(new ValidationError
                {
                    Code = "SCHEMA_NOT_FOUND",
                    Message = $"Schema version '{version}' not found",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = document?.SourcePath ?? "<unknown>",
                        Line = 1,
                        Column = 1
                    }
                });

                return new SchemaValidationResult
                {
                    IsValid = false,
                    SchemaVersion = version,
                    Errors = errors
                };
            }

            // Basic validation: check for required root properties
            // In Phase 1, we do simple structural validation
            if (document != null && (document.Trigger == null))
            {
                // Not a hard error, just warning (handled by syntax validator)
            }

            return new SchemaValidationResult
            {
                IsValid = true,
                SchemaVersion = version,
                Errors = Array.Empty<ValidationError>()
            };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "SCHEMA_VALIDATION_ERROR",
                Message = $"Error during schema validation: {ex.Message}",
                Severity = Severity.Error,
                Location = new SourceLocation
                {
                    FilePath = document?.SourcePath ?? "<unknown>",
                    Line = 1,
                    Column = 1
                }
            });

            return new SchemaValidationResult
            {
                IsValid = false,
                SchemaVersion = version,
                Errors = errors
            };
        }
    }

    private SchemaDefinition LoadSchemaInternal(string schemaSource)
    {
        if (_schemaCache.TryGetValue(schemaSource, out var cached))
        {
            return cached;
        }

        // For Phase 1, return a minimal schema definition
        // In real implementation, would load from file/web
        return new SchemaDefinition
        {
            Version = schemaSource,
            Schema = "https://raw.githubusercontent.com/microsoft/azure-pipelines-vscode/main/service-schema.json",
            Types = new Dictionary<string, TypeSchema>
            {
                { "Pipeline", new TypeSchema { Name = "Pipeline", Properties = new Dictionary<string, PropertySchema>().AsReadOnly() } }
            },
            RootType = "Pipeline"
        };
    }

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
