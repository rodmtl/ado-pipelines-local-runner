namespace AdoPipelinesLocalRunner.Contracts;

/// <summary>
/// Manages Azure DevOps pipeline schema definitions and validation.
/// </summary>
public interface ISchemaManager
{
    /// <summary>
    /// Validates a pipeline document against the schema.
    /// </summary>
    /// <param name="document">Pipeline document to validate</param>
    /// <param name="schemaVersion">Optional specific schema version (uses latest if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Schema validation result</returns>
    Task<SchemaValidationResult> ValidateAsync(
        PipelineDocument document,
        string? schemaVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a schema definition from a source.
    /// </summary>
    /// <param name="schemaSource">Schema source (URL, file path, or embedded resource)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded schema definition</returns>
    Task<SchemaDefinition> LoadSchemaAsync(string schemaSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current default schema version.
    /// </summary>
    /// <returns>Schema version string</returns>
    string GetDefaultSchemaVersion();

    /// <summary>
    /// Gets schema definition for a specific type.
    /// </summary>
    /// <param name="typeName">Type name (e.g., "jobs", "steps", "stage")</param>
    /// <param name="schemaVersion">Optional schema version</param>
    /// <returns>Type schema definition, or null if not found</returns>
    TypeSchema? GetTypeSchema(string typeName, string? schemaVersion = null);
}

/// <summary>
/// Result of schema validation.
/// </summary>
public record SchemaValidationResult
{
    /// <summary>
    /// Indicates whether the document conforms to the schema.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Schema version used for validation.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Collection of schema validation errors.
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }

    /// <summary>
    /// Additional validation metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a complete schema definition.
/// </summary>
public record SchemaDefinition
{
    /// <summary>
    /// Schema version identifier.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Schema URI or identifier.
    /// </summary>
    public required string Schema { get; init; }

    /// <summary>
    /// Type definitions in this schema.
    /// </summary>
    public required IReadOnlyDictionary<string, TypeSchema> Types { get; init; }

    /// <summary>
    /// Root type name for pipeline documents.
    /// </summary>
    public required string RootType { get; init; }

    /// <summary>
    /// Schema metadata and documentation.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Schema definition for a specific type.
/// </summary>
public record TypeSchema
{
    /// <summary>
    /// Type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Type description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Properties defined for this type.
    /// </summary>
    public required IReadOnlyDictionary<string, PropertySchema> Properties { get; init; }

    /// <summary>
    /// Required property names.
    /// </summary>
    public IReadOnlyList<string>? Required { get; init; }

    /// <summary>
    /// Whether additional properties are allowed.
    /// </summary>
    public bool AdditionalPropertiesAllowed { get; init; }
}

/// <summary>
/// Schema definition for a property.
/// </summary>
public record PropertySchema
{
    /// <summary>
    /// Property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Property description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Expected type(s) for this property.
    /// </summary>
    public required IReadOnlyList<string> Types { get; init; }

    /// <summary>
    /// Default value if not specified.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Allowed values (for enum-like properties).
    /// </summary>
    public IReadOnlyList<object>? AllowedValues { get; init; }

    /// <summary>
    /// Pattern for string validation (regex).
    /// </summary>
    public string? Pattern { get; init; }
}
