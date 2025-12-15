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
            if (document == null)
            {
                errors.Add(new ValidationError
                {
                    Code = "SCHEMA_INVALID_DOCUMENT",
                    Message = "Pipeline document is null",
                    Severity = Severity.Error,
                    Location = new SourceLocation
                    {
                        FilePath = document?.SourcePath ?? "<unknown>",
                        Line = 1,
                        Column = 1
                    }
                });
            }
            else
            {
                if (document.Trigger == null)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "SCHEMA_MISSING_TRIGGER",
                        Message = "Required property 'trigger' is missing",
                        Severity = Severity.Error,
                        Location = new SourceLocation
                        {
                            FilePath = document.SourcePath ?? "<unknown>",
                            Line = 1,
                            Column = 1
                        },
                        Suggestion = "Add a trigger section (e.g. 'trigger: none' or branches)."
                    });
                }

                if (document.Stages == null && document.Jobs == null && document.Steps == null)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "SCHEMA_MISSING_WORK",
                        Message = "Pipeline must define stages, jobs, or steps",
                        Severity = Severity.Error,
                        Location = new SourceLocation
                        {
                            FilePath = document.SourcePath ?? "<unknown>",
                            Line = 1,
                            Column = 1
                        },
                        Suggestion = "Add at least one stage, job, or step definition."
                    });
                }
            }

            return new SchemaValidationResult
            {
                IsValid = errors.Count == 0,
                SchemaVersion = version,
                Errors = errors.ToArray()
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
