namespace AdoPipelinesLocalRunner.Contracts;

/// <summary>
/// Parses YAML content into strongly-typed objects with source mapping support.
/// </summary>
public interface IYamlParser
{
    /// <summary>
    /// Parses YAML content from a string into a typed object.
    /// </summary>
    /// <typeparam name="T">The target type for deserialization</typeparam>
    /// <param name="content">Raw YAML content string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser result containing data, errors, and source mapping</returns>
    Task<ParserResult<T>> ParseAsync<T>(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses YAML content from a file into a typed object.
    /// </summary>
    /// <typeparam name="T">The target type for deserialization</typeparam>
    /// <param name="filePath">Path to the YAML file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser result containing data, errors, and source mapping</returns>
    Task<ParserResult<T>> ParseFileAsync<T>(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates YAML content structure without full deserialization.
    /// </summary>
    /// <param name="content">Raw YAML content string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parser result indicating structural validity</returns>
    Task<ParserResult<object>> ValidateStructureAsync(string content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a YAML parsing operation.
/// </summary>
/// <typeparam name="T">The type of parsed data</typeparam>
public record ParserResult<T>
{
    /// <summary>
    /// Indicates whether parsing succeeded without errors.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Parsed data object (null if parsing failed).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Collection of parsing errors encountered.
    /// </summary>
    public required IReadOnlyList<ParseError> Errors { get; init; }

    /// <summary>
    /// Source mapping for tracking original YAML locations.
    /// </summary>
    public required ISourceMap SourceMap { get; init; }
}

/// <summary>
/// Tracks mapping between parsed objects and YAML source locations.
/// </summary>
public interface ISourceMap
{
    /// <summary>
    /// Gets the line number for a specific property path.
    /// </summary>
    /// <param name="path">Dot-separated path to the property (e.g., "stages[0].jobs[1].steps[0]")</param>
    /// <returns>Line number in original YAML (1-based), or -1 if not found</returns>
    int GetLineNumber(string path);

    /// <summary>
    /// Gets the original source location for a given line number.
    /// </summary>
    /// <param name="line">Line number (1-based)</param>
    /// <returns>Source location details</returns>
    SourceLocation GetOriginalLocation(int line);

    /// <summary>
    /// Gets all property paths defined in the source.
    /// </summary>
    /// <returns>Collection of property paths</returns>
    IEnumerable<string> GetAllPaths();
}

/// <summary>
/// Represents a location in the source YAML file.
/// </summary>
public record SourceLocation
{
    /// <summary>
    /// File path of the source.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// Length of the relevant text span.
    /// </summary>
    public int Length { get; init; }
}
